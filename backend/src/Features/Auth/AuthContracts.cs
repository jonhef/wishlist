namespace Wishlist.Api.Features.Auth;

public sealed record RegisterRequest(string Email, string Password);

public sealed record RegisterResponse(Guid UserId, string Email);

public sealed record LoginRequest(string Email, string Password);

public sealed record RefreshRequest(string? RefreshToken);

public sealed record LogoutRequest(string? RefreshToken);

public sealed record AuthTokensResponse(
  string AccessToken,
  DateTime AccessTokenExpiresAtUtc,
  string? RefreshToken,
  DateTime RefreshTokenExpiresAtUtc);

public sealed record AuthErrorResponse(string Error);

public sealed record TokenPair(
  string AccessToken,
  DateTime AccessTokenExpiresAtUtc,
  string RefreshToken,
  DateTime RefreshTokenExpiresAtUtc);

public sealed record AuthServiceResult<T>(T? Value, string? ErrorCode, string? ErrorMessage)
{
  public bool IsSuccess => ErrorCode is null;

  public static AuthServiceResult<T> Success(T value) => new(value, null, null);

  public static AuthServiceResult<T> Failure(string errorCode, string errorMessage) =>
    new(default, errorCode, errorMessage);
}
