using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Wishlist.Api.Domain.Entities;

namespace Wishlist.Api.Features.Auth;

public sealed class TokenService(IOptions<AuthOptions> options, TimeProvider timeProvider) : ITokenService
{
  private readonly AuthOptions _options = options.Value;
  private readonly TimeProvider _timeProvider = timeProvider;

  public (string AccessToken, DateTime ExpiresAtUtc) CreateAccessToken(AppUser user)
  {
    if (_options.AccessTokenSecret.Length < 32)
    {
      throw new InvalidOperationException("Auth:AccessTokenSecret must be at least 32 characters long.");
    }

    var issuedAt = _timeProvider.GetUtcNow().UtcDateTime;
    var expiresAt = _options.AccessTokenTtlSeconds is > 0
      ? issuedAt.AddSeconds(_options.AccessTokenTtlSeconds.Value)
      : issuedAt.AddMinutes(_options.AccessTokenTtlMinutes);

    var claims = new List<Claim>
    {
      new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
      new(JwtRegisteredClaimNames.Email, user.Email),
      new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N")),
      new(JwtRegisteredClaimNames.Iat, EpochTime.GetIntDate(issuedAt).ToString(), ClaimValueTypes.Integer64)
    };

    var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.AccessTokenSecret));
    var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

    var token = new JwtSecurityToken(
      issuer: _options.Issuer,
      audience: _options.Audience,
      claims: claims,
      notBefore: issuedAt,
      expires: expiresAt,
      signingCredentials: credentials);

    var accessToken = new JwtSecurityTokenHandler().WriteToken(token);
    return (accessToken, expiresAt);
  }

  public (string PlainTextToken, RefreshToken RefreshToken) CreateRefreshToken(
    AppUser user,
    Guid? familyId,
    string? ipAddress,
    string? userAgent)
  {
    if (_options.RefreshTokenPepper.Length < 32)
    {
      throw new InvalidOperationException("Auth:RefreshTokenPepper must be at least 32 characters long.");
    }

    var tokenJti = Guid.NewGuid();
    var randomBytes = RandomNumberGenerator.GetBytes(48);
    var randomPart = WebEncoders.Base64UrlEncode(randomBytes);
    var plainTextToken = $"{tokenJti:N}.{randomPart}";

    var now = _timeProvider.GetUtcNow().UtcDateTime;
    var expiresAt = now.AddDays(_options.RefreshTokenTtlDays);

    var refreshToken = new RefreshToken
    {
      UserId = user.Id,
      Jti = tokenJti,
      FamilyId = familyId ?? Guid.NewGuid(),
      TokenHash = ComputeRefreshTokenHash(plainTextToken),
      CreatedAtUtc = now,
      ExpiresAtUtc = expiresAt,
      CreatedByIp = ipAddress,
      UserAgent = userAgent
    };

    return (plainTextToken, refreshToken);
  }

  public bool TryReadRefreshTokenJti(string refreshToken, out Guid jti)
  {
    jti = Guid.Empty;

    if (string.IsNullOrWhiteSpace(refreshToken))
    {
      return false;
    }

    var parts = refreshToken.Split('.', 2, StringSplitOptions.RemoveEmptyEntries);
    return parts.Length == 2 && Guid.TryParseExact(parts[0], "N", out jti);
  }

  public byte[] ComputeRefreshTokenHash(string refreshToken)
  {
    using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_options.RefreshTokenPepper));
    return hmac.ComputeHash(Encoding.UTF8.GetBytes(refreshToken));
  }
}
