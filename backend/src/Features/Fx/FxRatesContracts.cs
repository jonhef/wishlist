namespace Wishlist.Api.Features.Fx;

public sealed record FxRateValue(decimal EurPerQuote, string Source, DateOnly AsOf);

public sealed record FxRatesSnapshot(DateOnly LatestAsOf, IReadOnlyDictionary<string, FxRateValue> RatesByQuote);
