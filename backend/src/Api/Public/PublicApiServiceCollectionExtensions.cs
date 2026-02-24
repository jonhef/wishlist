using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

namespace Wishlist.Api.Api.Public;

public static class PublicApiServiceCollectionExtensions
{
  public static IServiceCollection AddPublicApi(this IServiceCollection services)
  {
    services.AddRateLimiter(options =>
    {
      options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
      options.AddFixedWindowLimiter(PublicRateLimitPolicies.PublicWishlistRead, limiterOptions =>
      {
        limiterOptions.PermitLimit = 60;
        limiterOptions.Window = TimeSpan.FromMinutes(1);
        limiterOptions.QueueLimit = 0;
        limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
      });
    });

    return services;
  }
}
