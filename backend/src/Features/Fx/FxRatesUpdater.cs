using System.Globalization;
using System.Text;
using System.Xml.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Wishlist.Api.Domain.Entities;
using Wishlist.Api.Infrastructure.Persistence;

namespace Wishlist.Api.Features.Fx;

public sealed class FxRatesUpdater(
  AppDbContext dbContext,
  IHttpClientFactory httpClientFactory,
  TimeProvider timeProvider,
  IHostEnvironment hostEnvironment,
  IOptions<FxRatesOptions> options,
  ILogger<FxRatesUpdater> logger) : IFxRatesUpdater
{
  private const string EcbUrl = "https://www.ecb.europa.eu/stats/eurofxref/eurofxref-daily.xml";
  private const string CbrUrl = "https://www.cbr.ru/scripts/XML_daily.asp";

  private readonly AppDbContext _dbContext = dbContext;
  private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;
  private readonly TimeProvider _timeProvider = timeProvider;
  private readonly IHostEnvironment _hostEnvironment = hostEnvironment;
  private readonly FxRatesOptions _options = options.Value;
  private readonly ILogger<FxRatesUpdater> _logger = logger;

  public async Task<bool> TryUpdateAsync(CancellationToken cancellationToken)
  {
    var lockAcquired = await TryAcquireAdvisoryLockAsync(cancellationToken);
    if (!lockAcquired)
    {
      _logger.LogInformation("Skipping FX update because advisory lock is held by another instance.");
      return false;
    }

    try
    {
      var todayUtc = DateOnly.FromDateTime(_timeProvider.GetUtcNow().UtcDateTime);
      if (await HasCompleteRatesForDateAsync(todayUtc, cancellationToken))
      {
        return true;
      }

      var ecbRates = await TryFetchEcbAsync(cancellationToken);
      var cbrRates = await TryFetchCbrAsync(cancellationToken);

      if (!TryBuildCanonicalRates(ecbRates, cbrRates, out var asOf, out var ratesByQuote))
      {
        var hasAnyRates = await _dbContext.FxRates
          .AsNoTracking()
          .AnyAsync(rate => rate.BaseCurrency == SupportedCurrencies.Eur, cancellationToken);

        if (!hasAnyRates && _hostEnvironment.IsDevelopment() && _options.SeedDevelopmentFallback)
        {
          await SeedFallbackRatesAsync(todayUtc, cancellationToken);
          _logger.LogWarning("External FX sources unavailable. Seeded development fallback rates.");
          return true;
        }

        _logger.LogWarning("FX update failed. External sources did not provide complete rates.");
        return false;
      }

      await UpsertRatesAsync(asOf, ratesByQuote, cancellationToken);
      _logger.LogInformation("FX rates updated for {AsOf}.", asOf);
      return true;
    }
    catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
    {
      throw;
    }
    catch (Exception ex)
    {
      _logger.LogWarning(ex, "FX rates update failed unexpectedly.");
      return false;
    }
    finally
    {
      await ReleaseAdvisoryLockAsync(cancellationToken);
    }
  }

  private async Task<bool> HasCompleteRatesForDateAsync(DateOnly asOf, CancellationToken cancellationToken)
  {
    var count = await _dbContext.FxRates
      .AsNoTracking()
      .Where(rate => rate.BaseCurrency == SupportedCurrencies.Eur && rate.AsOf == asOf)
      .Select(rate => rate.QuoteCurrency)
      .Distinct()
      .CountAsync(cancellationToken);

    return count >= SupportedCurrencies.All.Length;
  }

  private async Task UpsertRatesAsync(
    DateOnly asOf,
    IReadOnlyDictionary<string, (decimal EurPerQuote, string Source)> ratesByQuote,
    CancellationToken cancellationToken)
  {
    var existing = await _dbContext.FxRates
      .Where(rate => rate.BaseCurrency == SupportedCurrencies.Eur && rate.AsOf == asOf)
      .ToListAsync(cancellationToken);

    var existingByQuote = existing.ToDictionary(rate => rate.QuoteCurrency, StringComparer.Ordinal);
    var now = _timeProvider.GetUtcNow().UtcDateTime;

    foreach (var quote in SupportedCurrencies.All)
    {
      var payload = ratesByQuote[quote];
      if (existingByQuote.TryGetValue(quote, out var entity))
      {
        entity.RateToBase = payload.EurPerQuote;
        entity.Source = payload.Source;
        entity.UpdatedAtUtc = now;
        continue;
      }

      _dbContext.FxRates.Add(new FxRate
      {
        BaseCurrency = SupportedCurrencies.Eur,
        QuoteCurrency = quote,
        RateToBase = payload.EurPerQuote,
        AsOf = asOf,
        Source = payload.Source,
        UpdatedAtUtc = now
      });
    }

    await _dbContext.SaveChangesAsync(cancellationToken);
  }

  private async Task SeedFallbackRatesAsync(DateOnly asOf, CancellationToken cancellationToken)
  {
    var rates = new Dictionary<string, (decimal EurPerQuote, string Source)>(StringComparer.Ordinal)
    {
      [SupportedCurrencies.Eur] = (1m, "DEV_SEED"),
      [SupportedCurrencies.Usd] = (0.92m, "DEV_SEED"),
      [SupportedCurrencies.Jpy] = (0.0062m, "DEV_SEED"),
      [SupportedCurrencies.Rub] = (0.0102m, "DEV_SEED")
    };

    await UpsertRatesAsync(asOf, rates, cancellationToken);
  }

  private bool TryBuildCanonicalRates(
    EcbRates? ecbRates,
    CbrRates? cbrRates,
    out DateOnly asOf,
    out IReadOnlyDictionary<string, (decimal EurPerQuote, string Source)> rates)
  {
    asOf = default;
    rates = new Dictionary<string, (decimal EurPerQuote, string Source)>();

    if (ecbRates is null && cbrRates is null)
    {
      return false;
    }

    var values = new Dictionary<string, (decimal EurPerQuote, string Source)>(StringComparer.Ordinal)
    {
      [SupportedCurrencies.Eur] = (1m, ecbRates is not null ? "ECB" : "CBR_CROSS")
    };

    if (ecbRates is not null)
    {
      values[SupportedCurrencies.Usd] = (1m / ecbRates.UsdPerEur, "ECB");
      values[SupportedCurrencies.Jpy] = (1m / ecbRates.JpyPerEur, "ECB");
      asOf = ecbRates.AsOf;
    }

    if (cbrRates is not null)
    {
      values[SupportedCurrencies.Rub] = (1m / cbrRates.RubPerEur, "CBR");
      asOf = asOf == default || cbrRates.AsOf < asOf ? cbrRates.AsOf : asOf;

      if (ecbRates is null)
      {
        values[SupportedCurrencies.Usd] = (cbrRates.RubPerUsd / cbrRates.RubPerEur, "CBR_CROSS");
        values[SupportedCurrencies.Jpy] = (cbrRates.RubPerJpy / cbrRates.RubPerEur, "CBR_CROSS");
      }
    }

    if (values.Count < SupportedCurrencies.All.Length)
    {
      return false;
    }

    rates = values;
    return true;
  }

  private async Task<EcbRates?> TryFetchEcbAsync(CancellationToken cancellationToken)
  {
    try
    {
      var client = _httpClientFactory.CreateClient("fx-rates");
      var xml = await client.GetStringAsync(EcbUrl, cancellationToken);
      return ParseEcbRates(xml);
    }
    catch (Exception ex)
    {
      _logger.LogWarning(ex, "Failed to fetch ECB rates.");
      return null;
    }
  }

  private async Task<CbrRates?> TryFetchCbrAsync(CancellationToken cancellationToken)
  {
    try
    {
      var client = _httpClientFactory.CreateClient("fx-rates");
      var bytes = await client.GetByteArrayAsync(CbrUrl, cancellationToken);
      return ParseCbrRates(bytes);
    }
    catch (Exception ex)
    {
      _logger.LogWarning(ex, "Failed to fetch CBR rates.");
      return null;
    }
  }

  public static EcbRates? ParseEcbRates(string xml)
  {
    if (string.IsNullOrWhiteSpace(xml))
    {
      return null;
    }

    var doc = XDocument.Parse(xml);
    var datedCube = doc.Descendants()
      .FirstOrDefault(element => element.Name.LocalName == "Cube" && element.Attribute("time") is not null);

    if (datedCube is null)
    {
      return null;
    }

    if (!DateOnly.TryParseExact(datedCube.Attribute("time")!.Value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var asOf))
    {
      return null;
    }

    var usdPerEur = ParseCubeRate(datedCube, SupportedCurrencies.Usd);
    var jpyPerEur = ParseCubeRate(datedCube, SupportedCurrencies.Jpy);

    if (!usdPerEur.HasValue || !jpyPerEur.HasValue || usdPerEur.Value <= 0 || jpyPerEur.Value <= 0)
    {
      return null;
    }

    return new EcbRates(asOf, usdPerEur.Value, jpyPerEur.Value);
  }

  public static CbrRates? ParseCbrRates(byte[] xmlBytes)
  {
    if (xmlBytes.Length == 0)
    {
      return null;
    }

    Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    var xml = Encoding.GetEncoding("windows-1251").GetString(xmlBytes);
    var doc = XDocument.Parse(xml);

    var dateRaw = doc.Root?.Attribute("Date")?.Value;
    if (!DateOnly.TryParseExact(dateRaw, "dd.MM.yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var asOf))
    {
      return null;
    }

    decimal? rubPerEur = null;
    decimal? rubPerUsd = null;
    decimal? rubPerJpy = null;

    foreach (var valute in doc.Descendants().Where(element => element.Name.LocalName == "Valute"))
    {
      var code = valute.Elements().FirstOrDefault(element => element.Name.LocalName == "CharCode")?.Value?.Trim().ToUpperInvariant();
      var nominalRaw = valute.Elements().FirstOrDefault(element => element.Name.LocalName == "Nominal")?.Value?.Trim();
      var valueRaw = valute.Elements().FirstOrDefault(element => element.Name.LocalName == "Value")?.Value?.Trim();

      if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(nominalRaw) || string.IsNullOrWhiteSpace(valueRaw))
      {
        continue;
      }

      if (!int.TryParse(nominalRaw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var nominal) || nominal <= 0)
      {
        continue;
      }

      var normalizedValue = valueRaw.Replace(',', '.');
      if (!decimal.TryParse(normalizedValue, NumberStyles.Number, CultureInfo.InvariantCulture, out var value))
      {
        continue;
      }

      var rubPerUnit = value / nominal;

      if (code == SupportedCurrencies.Eur)
      {
        rubPerEur = rubPerUnit;
      }
      else if (code == SupportedCurrencies.Usd)
      {
        rubPerUsd = rubPerUnit;
      }
      else if (code == SupportedCurrencies.Jpy)
      {
        rubPerJpy = rubPerUnit;
      }
    }

    if (!rubPerEur.HasValue || !rubPerUsd.HasValue || !rubPerJpy.HasValue || rubPerEur <= 0 || rubPerUsd <= 0 || rubPerJpy <= 0)
    {
      return null;
    }

    return new CbrRates(asOf, rubPerEur.Value, rubPerUsd.Value, rubPerJpy.Value);
  }

  private static decimal? ParseCubeRate(XElement datedCube, string currency)
  {
    var node = datedCube.Elements()
      .FirstOrDefault(element => element.Name.LocalName == "Cube" && string.Equals(element.Attribute("currency")?.Value, currency, StringComparison.OrdinalIgnoreCase));

    if (node is null)
    {
      return null;
    }

    return decimal.TryParse(node.Attribute("rate")?.Value, NumberStyles.Number, CultureInfo.InvariantCulture, out var value)
      ? value
      : null;
  }

  private async Task<bool> TryAcquireAdvisoryLockAsync(CancellationToken cancellationToken)
  {
    if (!_dbContext.Database.IsNpgsql())
    {
      return true;
    }

    var acquired = await _dbContext.Database
      .SqlQueryRaw<bool>("SELECT pg_try_advisory_lock(hashtext('fx_rates_update')) AS \"Value\"")
      .SingleAsync(cancellationToken);

    return acquired;
  }

  private async Task ReleaseAdvisoryLockAsync(CancellationToken cancellationToken)
  {
    if (!_dbContext.Database.IsNpgsql())
    {
      return;
    }

    try
    {
      _ = await _dbContext.Database
        .SqlQueryRaw<bool>("SELECT pg_advisory_unlock(hashtext('fx_rates_update')) AS \"Value\"")
        .SingleAsync(cancellationToken);
    }
    catch (Exception ex)
    {
      _logger.LogWarning(ex, "Failed to release FX advisory lock.");
    }
  }

  public sealed record EcbRates(DateOnly AsOf, decimal UsdPerEur, decimal JpyPerEur);

  public sealed record CbrRates(DateOnly AsOf, decimal RubPerEur, decimal RubPerUsd, decimal RubPerJpy);
}
