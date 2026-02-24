namespace Wishlist.Api.Features.Items;

public static class ItemServiceCollectionExtensions
{
  public static IServiceCollection AddItemModule(this IServiceCollection services)
  {
    services.AddScoped<IItemService, ItemService>();
    return services;
  }
}
