namespace Wishlist.Api.Domain.Entities;

public sealed class WishlistEntity
{
  public Guid Id { get; set; } = Guid.NewGuid();

  public Guid OwnerUserId { get; set; }

  public AppUser OwnerUser { get; set; } = null!;

  public string Name { get; set; } = string.Empty;

  public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

  public ICollection<WishItem> Items { get; set; } = new List<WishItem>();
}
