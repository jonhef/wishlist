namespace Wishlist.Api.Features.Fx;

public interface IFxRatesUpdater
{
  Task<bool> TryUpdateAsync(CancellationToken cancellationToken);
}
