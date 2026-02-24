using Wishlist.Api.Features.Sharing;

namespace Wishlist.Api.Api.Public;

public static class PublicWishlistEndpoints
{
  public static IEndpointRouteBuilder MapPublicWishlistEndpoints(this IEndpointRouteBuilder endpoints)
  {
    var group = endpoints
      .MapGroup("/public")
      .RequireRateLimiting(PublicRateLimitPolicies.PublicWishlistRead);

    group.MapGet("/wishlists/{token}", GetByTokenAsync);

    return endpoints;
  }

  private static async Task<IResult> GetByTokenAsync(
    string token,
    IWishlistShareService wishlistShareService,
    CancellationToken cancellationToken)
  {
    var result = await wishlistShareService.GetPublicByTokenAsync(token, cancellationToken);

    if (!result.IsSuccess || result.Value is null)
    {
      return TypedResults.NotFound();
    }

    return TypedResults.Ok(result.Value);
  }
}

public static class PublicRateLimitPolicies
{
  public const string PublicWishlistRead = "public_wishlist_read";
}
