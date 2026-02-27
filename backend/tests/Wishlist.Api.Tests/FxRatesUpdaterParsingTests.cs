using Wishlist.Api.Features.Fx;

namespace Wishlist.Api.Tests;

public sealed class FxRatesUpdaterParsingTests
{
  [Fact]
  public void ParseEcbRates_ParsesUsdAndJpy()
  {
    const string xml = """
      <?xml version="1.0" encoding="UTF-8"?>
      <gesmes:Envelope xmlns:gesmes="http://www.gesmes.org/xml/2002-08-01" xmlns="http://www.ecb.int/vocabulary/2002-08-01/eurofxref">
        <Cube>
          <Cube time="2026-02-26">
            <Cube currency="USD" rate="1.0812"/>
            <Cube currency="JPY" rate="162.37"/>
          </Cube>
        </Cube>
      </gesmes:Envelope>
      """;

    var parsed = FxRatesUpdater.ParseEcbRates(xml);

    Assert.NotNull(parsed);
    Assert.Equal(new DateOnly(2026, 2, 26), parsed!.AsOf);
    Assert.Equal(1.0812m, parsed.UsdPerEur);
    Assert.Equal(162.37m, parsed.JpyPerEur);
  }

  [Fact]
  public void ParseCbrRates_ParsesRubCrossInputs()
  {
    const string xml = """
      <?xml version="1.0" encoding="windows-1251"?>
      <ValCurs Date="26.02.2026" Name="Foreign Currency Market">
        <Valute ID="R01239">
          <CharCode>EUR</CharCode>
          <Nominal>1</Nominal>
          <Value>102,5000</Value>
        </Valute>
        <Valute ID="R01235">
          <CharCode>USD</CharCode>
          <Nominal>1</Nominal>
          <Value>94,0000</Value>
        </Valute>
        <Valute ID="R01820">
          <CharCode>JPY</CharCode>
          <Nominal>100</Nominal>
          <Value>61,5000</Value>
        </Valute>
      </ValCurs>
      """;

    var parsed = FxRatesUpdater.ParseCbrRates(System.Text.Encoding.UTF8.GetBytes(xml));

    Assert.NotNull(parsed);
    Assert.Equal(new DateOnly(2026, 2, 26), parsed!.AsOf);
    Assert.Equal(102.5m, parsed.RubPerEur);
    Assert.Equal(94m, parsed.RubPerUsd);
    Assert.Equal(0.615m, parsed.RubPerJpy);
  }
}
