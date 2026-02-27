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
    PublicWishlistOrder? order,
    IWishlistShareService wishlistShareService,
    CancellationToken cancellationToken)
  {
    if (!Enum.IsDefined(sort) || (order.HasValue && !Enum.IsDefined(order.Value)))
    {
      return ApiProblem.Validation(
        httpContext,
        ApiProblem.SingleFieldError("sort", "Sort/order values are invalid."),
        "Validation failed.");
    }

    var resolvedOrder = order ?? PublicWishlistOrder.asc;

    var result = await wishlistShareService.GetPublicByTokenAsync(
      token,
      new PublicWishlistListQuery(cursor, limit, sort, resolvedOrder),
      cancellationToken);

    if (!result.IsSuccess || result.Value is null)
    {
      return result.ErrorCode switch
      {
        WishlistShareErrorCodes.NotFound => ApiProblem.NotFound(httpContext, "Wishlist not found."),
        WishlistShareErrorCodes.FxUnavailable => ApiProblem.Validation(
          httpContext,
          ApiProblem.SingleFieldError("sort", "Price sort is unavailable: exchange rates are missing."),
          "Validation failed."),
        _ => ApiProblem.NotFound(httpContext, "Wishlist not found.")
      };
    }

    return TypedResults.Ok(result.Value);
  }
}

public static class PublicRateLimitPolicies
{
  public const string PublicWishlistRead = "public_wishlist_read";
}
