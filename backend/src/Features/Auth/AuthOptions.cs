namespace Wishlist.Api.Features.Auth;

public sealed class AuthOptions
{
  public const string SectionName = "Auth";

  public string Issuer { get; init; } = "wishlist.api";

  public string Audience { get; init; } = "wishlist.web";

  public string AccessTokenSecret { get; init; } = "dev-access-secret-change-me-with-at-least-32-bytes";

  public string RefreshTokenPepper { get; init; } = "dev-refresh-pepper-change-me-with-at-least-32-bytes";

  public int AccessTokenTtlMinutes { get; init; } = 15;

  public int? AccessTokenTtlSeconds { get; init; }

  public int RefreshTokenTtlDays { get; init; } = 30;

  public bool UseRefreshCookie { get; init; }

  public string RefreshCookieName { get; init; } = "refreshToken";
}
