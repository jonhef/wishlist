using System.Globalization;

namespace Wishlist.Api.Features.Fx;

public static class SupportedCurrencies
{
  public const string Eur = "EUR";
  public const string Usd = "USD";
  public const string Rub = "RUB";
  public const string Jpy = "JPY";

  public static readonly string[] All = [Eur, Usd, Rub, Jpy];

  public static bool TryNormalize(string? value, out string? normalized)
  {
    normalized = null;

    if (string.IsNullOrWhiteSpace(value))
    {
      return false;
    }

    var uppercase = value.Trim().ToUpperInvariant();
    if (!All.Contains(uppercase, StringComparer.Ordinal))
    {
      return false;
    }

    normalized = uppercase;
    return true;
  }

  public static bool IsSupported(string? currency)
  {
    return currency is not null && All.Contains(currency, StringComparer.Ordinal);
  }

  public static int GetMinorUnits(string currency)
  {
    return currency switch
    {
      Eur => 2,
      Usd => 2,
      Rub => 2,
      Jpy => 0,
      _ => throw new ArgumentOutOfRangeException(nameof(currency), currency, "Unsupported currency.")
    };
  }

  public static decimal MinorToMajor(int amountMinor, string currency)
  {
    var minorUnits = GetMinorUnits(currency);
    if (minorUnits == 0)
    {
      return amountMinor;
    }

    var divisor = (decimal)Math.Pow(10, minorUnits);
    return amountMinor / divisor;
  }

  public static int MajorToMinor(decimal amountMajor, string currency)
  {
    var minorUnits = GetMinorUnits(currency);
    var scaled = amountMajor * (decimal)Math.Pow(10, minorUnits);
    return decimal.ToInt32(decimal.Round(scaled, 0, MidpointRounding.AwayFromZero));
  }

  public static string GetCurrencySymbol(string currency)
  {
    return currency switch
    {
      Eur => "€",
      Usd => "$",
      Rub => "₽",
      Jpy => "¥",
      _ => currency.ToUpper(CultureInfo.InvariantCulture)
    };
  }
}
