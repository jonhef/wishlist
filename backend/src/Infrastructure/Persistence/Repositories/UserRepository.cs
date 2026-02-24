using Microsoft.EntityFrameworkCore;
using Wishlist.Api.Domain.Entities;
using Wishlist.Api.Features.Auth;

namespace Wishlist.Api.Infrastructure.Persistence.Repositories;

public sealed class UserRepository(AppDbContext dbContext) : IUserRepository
{
  private readonly AppDbContext _dbContext = dbContext;

  public async Task<bool> ExistsByNormalizedEmailAsync(string normalizedEmail, CancellationToken cancellationToken)
  {
    return await _dbContext.Users.AnyAsync(
      user => user.NormalizedEmail == normalizedEmail,
      cancellationToken);
  }

  public async Task AddAsync(AppUser user, CancellationToken cancellationToken)
  {
    await _dbContext.Users.AddAsync(user, cancellationToken);
  }

  public async Task<AppUser?> FindByNormalizedEmailAsync(string normalizedEmail, CancellationToken cancellationToken)
  {
    return await _dbContext.Users
      .FirstOrDefaultAsync(user => user.NormalizedEmail == normalizedEmail, cancellationToken);
  }

  public async Task SaveChangesAsync(CancellationToken cancellationToken)
  {
    await _dbContext.SaveChangesAsync(cancellationToken);
  }
}
