using Microsoft.Extensions.Options;

namespace Wishlist.Api.Features.Fx;

public sealed class FxRatesHostedService(
  IServiceScopeFactory scopeFactory,
  IOptions<FxRatesOptions> options,
  ILogger<FxRatesHostedService> logger) : BackgroundService
{
  private readonly IServiceScopeFactory _scopeFactory = scopeFactory;
  private readonly FxRatesOptions _options = options.Value;
  private readonly ILogger<FxRatesHostedService> _logger = logger;

  protected override async Task ExecuteAsync(CancellationToken stoppingToken)
  {
    await TryUpdateOnceAsync(stoppingToken);

    var intervalHours = Math.Clamp(_options.RefreshIntervalHours, 1, 24);
    var interval = TimeSpan.FromHours(intervalHours);

    while (!stoppingToken.IsCancellationRequested)
    {
      try
      {
        await Task.Delay(interval, stoppingToken);
      }
      catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
      {
        break;
      }

      await TryUpdateOnceAsync(stoppingToken);
    }
  }

  private async Task TryUpdateOnceAsync(CancellationToken cancellationToken)
  {
    try
    {
      using var scope = _scopeFactory.CreateScope();
      var updater = scope.ServiceProvider.GetRequiredService<IFxRatesUpdater>();
      await updater.TryUpdateAsync(cancellationToken);
    }
    catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
    {
      throw;
    }
    catch (Exception ex)
    {
      _logger.LogWarning(ex, "FX background update cycle failed.");
    }
  }
}
