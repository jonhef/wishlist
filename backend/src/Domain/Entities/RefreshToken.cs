namespace Wishlist.Api.Domain.Entities;

public sealed class RefreshToken
{
  public Guid Id { get; set; } = Guid.NewGuid();

  public Guid UserId { get; set; }

  public AppUser User { get; set; } = null!;

  public Guid Jti { get; set; }

  public Guid FamilyId { get; set; }

  public byte[] TokenHash { get; set; } = Array.Empty<byte>();

  public DateTime ExpiresAtUtc { get; set; }

  public DateTime CreatedAtUtc { get; set; }

  public DateTime? RevokedAtUtc { get; set; }

  public Guid? ReplacedByJti { get; set; }

  public string? CreatedByIp { get; set; }

  public string? UserAgent { get; set; }
}
