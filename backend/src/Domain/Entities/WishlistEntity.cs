namespace Wishlist.Api.Domain.Entities;

public sealed class WishlistEntity
{
  public Guid Id { get; set; } = Guid.NewGuid();

  public Guid OwnerUserId { get; set; }

  public AppUser OwnerUser { get; set; } = null!;

  public string Title { get; set; } = string.Empty;

  public string? Description { get; set; }

  public Guid? ThemeId { get; set; }

  public ThemeEntity? Theme { get; set; }

  public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

  public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

  public bool IsDeleted { get; set; }

  public DateTime? DeletedAtUtc { get; set; }

  public string? ShareTokenHash { get; set; }

  public ICollection<WishItem> Items { get; set; } = new List<WishItem>();
}
