using Wishlist.Api.Features.Fx;

namespace Wishlist.Api.Tests;

public sealed class SupportedCurrenciesTests
{
  [Theory]
  [InlineData("EUR", 2)]
  [InlineData("USD", 2)]
  [InlineData("RUB", 2)]
  [InlineData("JPY", 0)]
  public void GetMinorUnits_ReturnsExpectedValues(string currency, int expected)
  {
    Assert.Equal(expected, SupportedCurrencies.GetMinorUnits(currency));
  }

  [Fact]
  public void PriceNormalization_HandlesDifferentCurrenciesAndMinorUnits()
  {
    var asOf = new DateOnly(2026, 2, 26);
    var rates = new Dictionary<string, FxRateValue>(StringComparer.Ordinal)
    {
      [SupportedCurrencies.Eur] = new(1m, "TEST", asOf),
      [SupportedCurrencies.Usd] = new(0.90m, "TEST", asOf),
      [SupportedCurrencies.Rub] = new(0.01m, "TEST", asOf),
      [SupportedCurrencies.Jpy] = new(0.006m, "TEST", asOf)
    };

    var usd = PriceNormalization.TryNormalizeMinorToBase(1999, "USD", "EUR", rates);
    var rub = PriceNormalization.TryNormalizeMinorToBase(150000, "RUB", "EUR", rates);
    var jpy = PriceNormalization.TryNormalizeMinorToBase(1200, "JPY", "EUR", rates);

    Assert.Equal(17.991m, usd);
    Assert.Equal(15m, rub);
    Assert.Equal(7.2m, jpy);
  }
}
