namespace Wishlist.Api.Domain.Entities;

public sealed class WishItem
{
  public int Id { get; set; }

  public Guid WishlistId { get; set; }

  public WishlistEntity Wishlist { get; set; } = null!;

  public string Name { get; set; } = string.Empty;

  public string? Url { get; set; }

  public decimal? PriceAmount { get; set; }

  public string? PriceCurrency { get; set; }

  public decimal Priority { get; set; }

  public string? Notes { get; set; }

  public DateTime CreatedAtUtc { get; set; }

  public DateTime UpdatedAtUtc { get; set; }

  public bool IsDeleted { get; set; }

  public DateTime? DeletedAtUtc { get; set; }
}
