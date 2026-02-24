using Wishlist.Api.Domain.Entities;

namespace Wishlist.Api.Features.Auth;

public interface IUserRepository
{
  Task<bool> ExistsByNormalizedEmailAsync(string normalizedEmail, CancellationToken cancellationToken);

  Task AddAsync(AppUser user, CancellationToken cancellationToken);

  Task<AppUser?> FindByNormalizedEmailAsync(string normalizedEmail, CancellationToken cancellationToken);

  Task SaveChangesAsync(CancellationToken cancellationToken);
}
