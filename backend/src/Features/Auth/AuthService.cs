using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Identity;
using Wishlist.Api.Domain.Entities;

namespace Wishlist.Api.Features.Auth;

public sealed class AuthService(
  IUserRepository userRepository,
  IRefreshTokenRepository refreshTokenRepository,
  IPasswordHasher<AppUser> passwordHasher,
  ITokenService tokenService,
  TimeProvider timeProvider) : IAuthService
{
  private static readonly EmailAddressAttribute EmailValidator = new();
  private readonly IUserRepository _userRepository = userRepository;
  private readonly IRefreshTokenRepository _refreshTokenRepository = refreshTokenRepository;
  private readonly IPasswordHasher<AppUser> _passwordHasher = passwordHasher;
  private readonly ITokenService _tokenService = tokenService;
  private readonly TimeProvider _timeProvider = timeProvider;

  public async Task<AuthServiceResult<RegisterResponse>> RegisterAsync(
    RegisterRequest request,
    CancellationToken cancellationToken)
  {
    if (!IsValidEmail(request.Email))
    {
      return AuthServiceResult<RegisterResponse>.Failure("invalid_email", "Email format is invalid.");
    }

    if (!IsValidPassword(request.Password))
    {
      return AuthServiceResult<RegisterResponse>.Failure(
        "invalid_password",
        "Password must be at least 12 characters long.");
    }

    var normalizedEmail = NormalizeEmail(request.Email);

    if (await _userRepository.ExistsByNormalizedEmailAsync(normalizedEmail, cancellationToken))
    {
      return AuthServiceResult<RegisterResponse>.Failure("email_already_exists", "Email already exists.");
    }

    var user = new AppUser
    {
      Email = request.Email.Trim(),
      NormalizedEmail = normalizedEmail,
      CreatedAtUtc = _timeProvider.GetUtcNow().UtcDateTime
    };

    user.PasswordHash = _passwordHasher.HashPassword(user, request.Password);

    await _userRepository.AddAsync(user, cancellationToken);
    await _userRepository.SaveChangesAsync(cancellationToken);

    return AuthServiceResult<RegisterResponse>.Success(new RegisterResponse(user.Id, user.Email));
  }

  public async Task<AuthServiceResult<TokenPair>> LoginAsync(
    LoginRequest request,
    string? ipAddress,
    string? userAgent,
    CancellationToken cancellationToken)
  {
    if (!IsValidEmail(request.Email) || string.IsNullOrWhiteSpace(request.Password))
    {
      return AuthServiceResult<TokenPair>.Failure("invalid_credentials", "Invalid credentials.");
    }

    var normalizedEmail = NormalizeEmail(request.Email);
    var user = await _userRepository.FindByNormalizedEmailAsync(normalizedEmail, cancellationToken);

    if (user is null)
    {
      return AuthServiceResult<TokenPair>.Failure("invalid_credentials", "Invalid credentials.");
    }

    var verificationResult = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);

    if (verificationResult == PasswordVerificationResult.Failed)
    {
      return AuthServiceResult<TokenPair>.Failure("invalid_credentials", "Invalid credentials.");
    }

    var (accessToken, accessTokenExpiresAtUtc) = _tokenService.CreateAccessToken(user);
    var (refreshTokenValue, refreshTokenEntity) =
      _tokenService.CreateRefreshToken(user, familyId: null, ipAddress, userAgent);

    await _refreshTokenRepository.AddAsync(refreshTokenEntity, cancellationToken);
    await _refreshTokenRepository.SaveChangesAsync(cancellationToken);

    return AuthServiceResult<TokenPair>.Success(
      new TokenPair(
        accessToken,
        accessTokenExpiresAtUtc,
        refreshTokenValue,
        refreshTokenEntity.ExpiresAtUtc));
  }

  public async Task<AuthServiceResult<TokenPair>> RefreshAsync(
    string? refreshToken,
    string? ipAddress,
    string? userAgent,
    CancellationToken cancellationToken)
  {
    if (string.IsNullOrWhiteSpace(refreshToken))
    {
      return AuthServiceResult<TokenPair>.Failure("invalid_refresh_token", "Refresh token is invalid.");
    }

    if (!_tokenService.TryReadRefreshTokenJti(refreshToken, out var tokenJti))
    {
      return AuthServiceResult<TokenPair>.Failure("invalid_refresh_token", "Refresh token is invalid.");
    }

    var existingToken = await _refreshTokenRepository.FindByJtiAsync(tokenJti, cancellationToken);

    if (existingToken is null)
    {
      return AuthServiceResult<TokenPair>.Failure("invalid_refresh_token", "Refresh token is invalid.");
    }

    if (!TokenHashMatches(existingToken.TokenHash, refreshToken))
    {
      return AuthServiceResult<TokenPair>.Failure("invalid_refresh_token", "Refresh token is invalid.");
    }

    var now = _timeProvider.GetUtcNow().UtcDateTime;

    if (existingToken.RevokedAtUtc is not null)
    {
      await _refreshTokenRepository.RevokeFamilyAsync(existingToken.FamilyId, now, cancellationToken);
      await _refreshTokenRepository.SaveChangesAsync(cancellationToken);
      return AuthServiceResult<TokenPair>.Failure("invalid_refresh_token", "Refresh token is invalid.");
    }

    if (existingToken.ExpiresAtUtc <= now)
    {
      return AuthServiceResult<TokenPair>.Failure("invalid_refresh_token", "Refresh token is invalid.");
    }

    var user = existingToken.User;
    var (nextAccessToken, nextAccessTokenExpiresAtUtc) = _tokenService.CreateAccessToken(user);
    var (nextRefreshTokenValue, nextRefreshTokenEntity) =
      _tokenService.CreateRefreshToken(user, existingToken.FamilyId, ipAddress, userAgent);

    existingToken.RevokedAtUtc = now;
    existingToken.ReplacedByJti = nextRefreshTokenEntity.Jti;

    await _refreshTokenRepository.AddAsync(nextRefreshTokenEntity, cancellationToken);
    await _refreshTokenRepository.SaveChangesAsync(cancellationToken);

    return AuthServiceResult<TokenPair>.Success(
      new TokenPair(
        nextAccessToken,
        nextAccessTokenExpiresAtUtc,
        nextRefreshTokenValue,
        nextRefreshTokenEntity.ExpiresAtUtc));
  }

  public async Task<AuthServiceResult<bool>> LogoutAsync(string? refreshToken, CancellationToken cancellationToken)
  {
    if (string.IsNullOrWhiteSpace(refreshToken))
    {
      return AuthServiceResult<bool>.Success(true);
    }

    if (!_tokenService.TryReadRefreshTokenJti(refreshToken, out var tokenJti))
    {
      return AuthServiceResult<bool>.Success(true);
    }

    var existingToken = await _refreshTokenRepository.FindByJtiAsync(tokenJti, cancellationToken);

    if (existingToken is null)
    {
      return AuthServiceResult<bool>.Success(true);
    }

    if (!TokenHashMatches(existingToken.TokenHash, refreshToken))
    {
      return AuthServiceResult<bool>.Success(true);
    }

    if (existingToken.RevokedAtUtc is null)
    {
      existingToken.RevokedAtUtc = _timeProvider.GetUtcNow().UtcDateTime;
      await _refreshTokenRepository.SaveChangesAsync(cancellationToken);
    }

    return AuthServiceResult<bool>.Success(true);
  }

  private bool TokenHashMatches(byte[] storedHash, string refreshToken)
  {
    var incomingHash = _tokenService.ComputeRefreshTokenHash(refreshToken);
    return storedHash.Length == incomingHash.Length
      && CryptographicOperations.FixedTimeEquals(storedHash, incomingHash);
  }

  private static bool IsValidEmail(string email) =>
    !string.IsNullOrWhiteSpace(email) && EmailValidator.IsValid(email.Trim());

  private static bool IsValidPassword(string password) =>
    !string.IsNullOrWhiteSpace(password) && password.Trim().Length >= 12;

  private static string NormalizeEmail(string email) => email.Trim().ToUpperInvariant();
}
