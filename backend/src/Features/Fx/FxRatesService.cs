using Microsoft.EntityFrameworkCore;
using Wishlist.Api.Infrastructure.Persistence;

namespace Wishlist.Api.Features.Fx;

public sealed class FxRatesService(AppDbContext dbContext) : IFxRatesService
{
  private readonly AppDbContext _dbContext = dbContext;

  public async Task<FxRatesSnapshot?> GetLatestSnapshotAsync(CancellationToken cancellationToken)
  {
    var rates = new Dictionary<string, FxRateValue>(StringComparer.Ordinal);

    foreach (var quote in SupportedCurrencies.All)
    {
      var latest = await _dbContext.FxRates
        .AsNoTracking()
        .Where(rate => rate.BaseCurrency == SupportedCurrencies.Eur && rate.QuoteCurrency == quote)
        .OrderByDescending(rate => rate.AsOf)
        .FirstOrDefaultAsync(cancellationToken);

      if (latest is null)
      {
        continue;
      }

      rates[quote] = new FxRateValue(latest.RateToBase, latest.Source, latest.AsOf);
    }

    if (rates.Count == 0)
    {
      return null;
    }

    var latestAsOf = rates.Values.Max(value => value.AsOf);
    return new FxRatesSnapshot(latestAsOf, rates);
  }
}
