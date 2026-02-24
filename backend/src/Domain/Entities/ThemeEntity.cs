namespace Wishlist.Api.Domain.Entities;

public sealed class ThemeEntity
{
  public Guid Id { get; set; } = Guid.NewGuid();

  public Guid OwnerUserId { get; set; }

  public AppUser OwnerUser { get; set; } = null!;

  public string Name { get; set; } = string.Empty;

  public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

  public ICollection<WishlistEntity> Wishlists { get; set; } = new List<WishlistEntity>();
}
