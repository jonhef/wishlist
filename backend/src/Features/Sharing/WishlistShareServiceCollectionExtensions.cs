namespace Wishlist.Api.Features.Sharing;

public static class WishlistShareServiceCollectionExtensions
{
  public static IServiceCollection AddWishlistSharingModule(this IServiceCollection services)
  {
    services.AddScoped<IWishlistShareService, WishlistShareService>();
    return services;
  }
}
