namespace Wishlist.Api.Features.Wishlists;

public interface IWishlistService
{
  Task<WishlistServiceResult<WishlistDto>> CreateAsync(
    Guid ownerUserId,
    CreateWishlistRequestDto request,
    CancellationToken cancellationToken);

  Task<WishlistServiceResult<WishlistListResult>> ListAsync(
    Guid ownerUserId,
    WishlistListQuery query,
    CancellationToken cancellationToken);

  Task<WishlistServiceResult<WishlistDto>> GetByIdAsync(
    Guid ownerUserId,
    Guid wishlistId,
    CancellationToken cancellationToken);

  Task<WishlistServiceResult<WishlistDto>> UpdateAsync(
    Guid ownerUserId,
    Guid wishlistId,
    UpdateWishlistRequestDto request,
    CancellationToken cancellationToken);

  Task<WishlistServiceResult<bool>> DeleteAsync(
    Guid ownerUserId,
    Guid wishlistId,
    CancellationToken cancellationToken);
}
