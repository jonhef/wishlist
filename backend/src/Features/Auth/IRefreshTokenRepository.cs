using Wishlist.Api.Domain.Entities;

namespace Wishlist.Api.Features.Auth;

public interface IRefreshTokenRepository
{
  Task AddAsync(RefreshToken refreshToken, CancellationToken cancellationToken);

  Task<RefreshToken?> FindByJtiAsync(Guid jti, CancellationToken cancellationToken);

  Task RevokeFamilyAsync(Guid familyId, DateTime revokedAtUtc, CancellationToken cancellationToken);

  Task SaveChangesAsync(CancellationToken cancellationToken);
}
