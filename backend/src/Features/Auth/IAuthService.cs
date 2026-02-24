namespace Wishlist.Api.Features.Auth;

public interface IAuthService
{
  Task<AuthServiceResult<RegisterResponse>> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken);

  Task<AuthServiceResult<TokenPair>> LoginAsync(
    LoginRequest request,
    string? ipAddress,
    string? userAgent,
    CancellationToken cancellationToken);

  Task<AuthServiceResult<TokenPair>> RefreshAsync(
    string? refreshToken,
    string? ipAddress,
    string? userAgent,
    CancellationToken cancellationToken);

  Task<AuthServiceResult<bool>> LogoutAsync(string? refreshToken, CancellationToken cancellationToken);
}
