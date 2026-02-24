namespace Wishlist.Api.Features.Wishlists;

public static class WishlistServiceCollectionExtensions
{
  public static IServiceCollection AddWishlistModule(this IServiceCollection services)
  {
    services.AddScoped<IWishlistService, WishlistService>();
    return services;
  }
}
