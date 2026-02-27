namespace Wishlist.Api.Features.Fx;

public sealed class FxRatesOptions
{
  public const string SectionName = "FxRates";

  public int RefreshIntervalHours { get; set; } = 6;

  public bool SeedDevelopmentFallback { get; set; } = true;
}
