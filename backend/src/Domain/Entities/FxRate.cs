namespace Wishlist.Api.Domain.Entities;

public sealed class FxRate
{
  public string BaseCurrency { get; set; } = "EUR";

  public string QuoteCurrency { get; set; } = "EUR";

  public decimal RateToBase { get; set; }

  public DateOnly AsOf { get; set; }

  public string Source { get; set; } = string.Empty;

  public DateTime UpdatedAtUtc { get; set; }
}
