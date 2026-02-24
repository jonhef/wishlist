using Microsoft.EntityFrameworkCore;
using Wishlist.Api.Domain.Entities;
using Wishlist.Api.Features.Auth;

namespace Wishlist.Api.Infrastructure.Persistence.Repositories;

public sealed class RefreshTokenRepository(AppDbContext dbContext) : IRefreshTokenRepository
{
  private readonly AppDbContext _dbContext = dbContext;

  public async Task AddAsync(RefreshToken refreshToken, CancellationToken cancellationToken)
  {
    await _dbContext.RefreshTokens.AddAsync(refreshToken, cancellationToken);
  }

  public async Task<RefreshToken?> FindByJtiAsync(Guid jti, CancellationToken cancellationToken)
  {
    return await _dbContext.RefreshTokens
      .Include(token => token.User)
      .FirstOrDefaultAsync(token => token.Jti == jti, cancellationToken);
  }

  public async Task RevokeFamilyAsync(Guid familyId, DateTime revokedAtUtc, CancellationToken cancellationToken)
  {
    await _dbContext.RefreshTokens
      .Where(token => token.FamilyId == familyId && token.RevokedAtUtc == null)
      .ExecuteUpdateAsync(
        setters => setters.SetProperty(token => token.RevokedAtUtc, revokedAtUtc),
        cancellationToken);
  }

  public async Task SaveChangesAsync(CancellationToken cancellationToken)
  {
    await _dbContext.SaveChangesAsync(cancellationToken);
  }
}
