namespace Wishlist.Api.Features.Items;

public interface IItemService
{
  Task<ItemServiceResult<ItemDto>> CreateAsync(
    Guid ownerUserId,
    Guid wishlistId,
    CreateItemRequestDto request,
    CancellationToken cancellationToken);

  Task<ItemServiceResult<ItemListResult>> ListAsync(
    Guid ownerUserId,
    Guid wishlistId,
    ItemListQuery query,
    CancellationToken cancellationToken);

  Task<ItemServiceResult<ItemDto>> UpdateAsync(
    Guid ownerUserId,
    Guid wishlistId,
    int itemId,
    UpdateItemRequestDto request,
    CancellationToken cancellationToken);

  Task<ItemServiceResult<bool>> DeleteAsync(
    Guid ownerUserId,
    Guid wishlistId,
    int itemId,
    CancellationToken cancellationToken);

  Task<ItemServiceResult<RebalanceItemsResultDto>> RebalanceAsync(
    Guid ownerUserId,
    Guid wishlistId,
    CancellationToken cancellationToken);
}
