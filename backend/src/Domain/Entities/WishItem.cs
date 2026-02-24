namespace Wishlist.Api.Domain.Entities;

public sealed class WishItem
{
  public int Id { get; set; }

  public Guid WishlistId { get; set; }

  public WishlistEntity Wishlist { get; set; } = null!;

  public string Title { get; set; } = string.Empty;

  public DateTime CreatedAtUtc { get; set; }
}
