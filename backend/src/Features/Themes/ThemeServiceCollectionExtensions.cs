namespace Wishlist.Api.Features.Themes;

public static class ThemeServiceCollectionExtensions
{
  public static IServiceCollection AddThemeModule(this IServiceCollection services)
  {
    services.AddScoped<IThemeService, ThemeService>();
    return services;
  }
}
