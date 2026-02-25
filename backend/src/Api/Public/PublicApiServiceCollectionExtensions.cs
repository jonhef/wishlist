using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using Wishlist.Api.Api.Errors;

namespace Wishlist.Api.Api.Public;

public static class PublicApiServiceCollectionExtensions
{
  public static IServiceCollection AddPublicApi(this IServiceCollection services)
  {
    services.AddRateLimiter(options =>
    {
      options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
      options.OnRejected = async (context, _) =>
      {
        if (!context.HttpContext.Response.HasStarted)
        {
          await ApiProblem.TooManyRequests(context.HttpContext, "Rate limit exceeded.")
            .ExecuteAsync(context.HttpContext);
        }
      };
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
