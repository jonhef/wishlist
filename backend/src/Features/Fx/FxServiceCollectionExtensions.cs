namespace Wishlist.Api.Features.Fx;

public static class FxServiceCollectionExtensions
{
  public static IServiceCollection AddFxModule(this IServiceCollection services, IConfiguration configuration)
  {
    services.Configure<FxRatesOptions>(configuration.GetSection(FxRatesOptions.SectionName));
    services.AddHttpClient("fx-rates", (_, client) =>
    {
      client.Timeout = TimeSpan.FromSeconds(30);
    });

    services.AddScoped<IFxRatesService, FxRatesService>();
    services.AddScoped<IFxRatesUpdater, FxRatesUpdater>();
    services.AddHostedService<FxRatesHostedService>();
    return services;
  }
}
