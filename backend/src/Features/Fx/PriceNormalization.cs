namespace Wishlist.Api.Features.Fx;

public static class PriceNormalization
{
  public static decimal? TryNormalizeMinorToBase(
    int? amountMinor,
    string? sourceCurrency,
    string wishlistBaseCurrency,
    IReadOnlyDictionary<string, FxRateValue> ratesByQuote)
  {
    if (!amountMinor.HasValue || string.IsNullOrWhiteSpace(sourceCurrency) || string.IsNullOrWhiteSpace(wishlistBaseCurrency))
    {
      return null;
    }

    var source = sourceCurrency.Trim().ToUpperInvariant();
    var baseCurrency = wishlistBaseCurrency.Trim().ToUpperInvariant();

    if (!SupportedCurrencies.IsSupported(source) || !SupportedCurrencies.IsSupported(baseCurrency))
    {
      return null;
    }

    if (!ratesByQuote.TryGetValue(source, out var sourceRate) || !ratesByQuote.TryGetValue(baseCurrency, out var baseRate))
    {
      return null;
    }

    var amountMajor = SupportedCurrencies.MinorToMajor(amountMinor.Value, source);
    return amountMajor * sourceRate.EurPerQuote / baseRate.EurPerQuote;
  }
}
