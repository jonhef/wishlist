namespace Wishlist.Api.Domain.Entities;

public sealed class WishItem
{
  public int Id { get; set; }

  public string Title { get; set; } = string.Empty;

  public DateTime CreatedAtUtc { get; set; }
}
