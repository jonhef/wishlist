namespace Wishlist.Api.Features.Fx;

public interface IFxRatesService
{
  Task<FxRatesSnapshot?> GetLatestSnapshotAsync(CancellationToken cancellationToken);
}
