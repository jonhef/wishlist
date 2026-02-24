using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Wishlist.Api.Infrastructure.Persistence;

namespace Wishlist.Api.Features.Sharing;

public sealed class WishlistShareService(AppDbContext dbContext) : IWishlistShareService
{
  private readonly AppDbContext _dbContext = dbContext;

  public async Task<WishlistShareServiceResult<ShareRotationResult>> RotateAsync(
    Guid ownerUserId,
    Guid wishlistId,
    CancellationToken cancellationToken)
  {
    var wishlist = await _dbContext.Wishlists
      .FirstOrDefaultAsync(x => x.Id == wishlistId && !x.IsDeleted, cancellationToken);

    if (wishlist is null)
    {
      return WishlistShareServiceResult<ShareRotationResult>.Failure(WishlistShareErrorCodes.NotFound);
    }

    if (wishlist.OwnerUserId != ownerUserId)
    {
      return WishlistShareServiceResult<ShareRotationResult>.Failure(WishlistShareErrorCodes.Forbidden);
    }

    var token = GenerateToken();
    wishlist.ShareTokenHash = ComputeTokenHash(token);

    await _dbContext.SaveChangesAsync(cancellationToken);

    return WishlistShareServiceResult<ShareRotationResult>.Success(new ShareRotationResult(token));
  }

  public async Task<WishlistShareServiceResult<bool>> DisableAsync(
    Guid ownerUserId,
    Guid wishlistId,
    CancellationToken cancellationToken)
  {
    var wishlist = await _dbContext.Wishlists
      .FirstOrDefaultAsync(x => x.Id == wishlistId && !x.IsDeleted, cancellationToken);

    if (wishlist is null)
    {
      return WishlistShareServiceResult<bool>.Failure(WishlistShareErrorCodes.NotFound);
    }

    if (wishlist.OwnerUserId != ownerUserId)
    {
      return WishlistShareServiceResult<bool>.Failure(WishlistShareErrorCodes.Forbidden);
    }

    wishlist.ShareTokenHash = null;
    await _dbContext.SaveChangesAsync(cancellationToken);

    return WishlistShareServiceResult<bool>.Success(true);
  }

  public async Task<WishlistShareServiceResult<PublicWishlistDto>> GetPublicByTokenAsync(
    string token,
    CancellationToken cancellationToken)
  {
    if (string.IsNullOrWhiteSpace(token))
    {
      return WishlistShareServiceResult<PublicWishlistDto>.Failure(WishlistShareErrorCodes.NotFound);
    }

    var tokenHash = ComputeTokenHash(token);

    var wishlist = await _dbContext.Wishlists
      .AsNoTracking()
      .FirstOrDefaultAsync(
        x => x.ShareTokenHash == tokenHash && !x.IsDeleted,
        cancellationToken);

    if (wishlist is null)
    {
      return WishlistShareServiceResult<PublicWishlistDto>.Failure(WishlistShareErrorCodes.NotFound);
    }

    var items = await _dbContext.WishItems
      .AsNoTracking()
      .Where(x => x.WishlistId == wishlist.Id && !x.IsDeleted)
      .OrderByDescending(x => x.UpdatedAtUtc)
      .ThenByDescending(x => x.Id)
      .Select(x => new PublicWishlistItemDto(
        x.Name,
        x.Url,
        x.PriceAmount,
        x.PriceCurrency,
        x.Priority,
        x.Notes))
      .ToListAsync(cancellationToken);

    var payload = new PublicWishlistDto(
      wishlist.Title,
      wishlist.Description,
      items);

    return WishlistShareServiceResult<PublicWishlistDto>.Success(payload);
  }

  private static string GenerateToken()
  {
    var bytes = RandomNumberGenerator.GetBytes(32);
    return WebEncoders.Base64UrlEncode(bytes);
  }

  private static string ComputeTokenHash(string token)
  {
    var bytes = Encoding.UTF8.GetBytes(token);
    var hash = SHA256.HashData(bytes);
    return Convert.ToHexString(hash);
  }
}
