namespace Wishlist.Api.Domain.Entities;

public sealed class AppUser
{
  public Guid Id { get; set; } = Guid.NewGuid();

  public string Email { get; set; } = string.Empty;

  public string NormalizedEmail { get; set; } = string.Empty;

  public string PasswordHash { get; set; } = string.Empty;

  public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

  public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();

  public ICollection<WishlistEntity> Wishlists { get; set; } = new List<WishlistEntity>();
}
