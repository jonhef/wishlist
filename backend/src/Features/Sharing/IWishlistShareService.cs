namespace Wishlist.Api.Features.Sharing;

public interface IWishlistShareService
{
  Task<WishlistShareServiceResult<ShareRotationResult>> RotateAsync(
    Guid ownerUserId,
    Guid wishlistId,
    CancellationToken cancellationToken);

  Task<WishlistShareServiceResult<bool>> DisableAsync(
    Guid ownerUserId,
    Guid wishlistId,
    CancellationToken cancellationToken);

  Task<WishlistShareServiceResult<PublicWishlistDto>> GetPublicByTokenAsync(
    string token,
    PublicWishlistListQuery query,
    CancellationToken cancellationToken);
}
