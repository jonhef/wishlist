using Wishlist.Api.Domain.Entities;

namespace Wishlist.Api.Features.Auth;

public interface ITokenService
{
  (string AccessToken, DateTime ExpiresAtUtc) CreateAccessToken(AppUser user);

  (string PlainTextToken, RefreshToken RefreshToken) CreateRefreshToken(
    AppUser user,
    Guid? familyId,
    string? ipAddress,
    string? userAgent);

  bool TryReadRefreshTokenJti(string refreshToken, out Guid jti);

  byte[] ComputeRefreshTokenHash(string refreshToken);
}
