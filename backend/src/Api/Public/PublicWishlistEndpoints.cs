using Wishlist.Api.Features.Sharing;
using Wishlist.Api.Api.Errors;

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
    HttpContext httpContext,
    string token,
    string? cursor,
    int? limit,
    PublicWishlistSort sort,
    IWishlistShareService wishlistShareService,
    CancellationToken cancellationToken)
  {
    if (!Enum.IsDefined(sort))
    {
      return ApiProblem.Validation(
        httpContext,
        ApiProblem.SingleFieldError("sort", "Sort must be one of: priority, added."),
        "Validation failed.");
    }

    var result = await wishlistShareService.GetPublicByTokenAsync(
      token,
      new PublicWishlistListQuery(cursor, limit, sort),
      cancellationToken);

    if (!result.IsSuccess || result.Value is null)
    {
      return ApiProblem.NotFound(httpContext, "Wishlist not found.");
    }

    return TypedResults.Ok(result.Value);
  }
}

public static class PublicRateLimitPolicies
{
  public const string PublicWishlistRead = "public_wishlist_read";
}
