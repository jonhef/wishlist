using Wishlist.Api.Api.Auth;
using Wishlist.Api.Api.Errors;
using Wishlist.Api.Features.Wishlists;

namespace Wishlist.Api.Api.Wishlists;

public static class WishlistEndpoints
{
  public static IEndpointRouteBuilder MapWishlistEndpoints(this IEndpointRouteBuilder endpoints)
  {
    MapCrud(endpoints, "/wishlists");
    MapCrud(endpoints, "/api/wishlists");
    return endpoints;
  }

  private static void MapCrud(IEndpointRouteBuilder endpoints, string prefix)
  {
    var group = endpoints.MapGroup(prefix).RequireAuthorization();
    group.MapPost("/", CreateAsync);
    group.MapGet("/", ListAsync);
    group.MapGet("/{wishlistId:guid}", GetWishlistAsync);
    group.MapPatch("/{wishlistId:guid}", PatchAsync);
    group.MapDelete("/{wishlistId:guid}", DeleteAsync);
    group.MapWishlistItemCrud();
    group.MapWishlistShareManagement();
  }

  private static async Task<IResult> CreateAsync(
    HttpContext httpContext,
    CreateWishlistRequestDto request,
    IWishlistService wishlistService,
    ICurrentUserAccessor currentUserAccessor,
    CancellationToken cancellationToken)
  {
    var result = await wishlistService.CreateAsync(
      currentUserAccessor.GetRequiredUserId(),
      request,
      cancellationToken);

    return result.ErrorCode switch
    {
      null => TypedResults.Created($"/wishlists/{result.Value!.Id}", result.Value),
      WishlistErrorCodes.Forbidden => ApiProblem.Forbidden(httpContext, "Access denied."),
      WishlistErrorCodes.ThemeNotAccessible => ApiProblem.Validation(
        httpContext,
        ApiProblem.SingleFieldError("themeId", "themeId is not accessible."),
        "Validation failed."),
      _ => ApiProblem.Validation(
        httpContext,
        ApiProblem.RequestError("Validation failed."),
        "Validation failed.")
    };
  }

  private static async Task<IResult> ListAsync(
    string? cursor,
    int? limit,
    IWishlistService wishlistService,
    ICurrentUserAccessor currentUserAccessor,
    CancellationToken cancellationToken)
  {
    var result = await wishlistService.ListAsync(
      currentUserAccessor.GetRequiredUserId(),
      new WishlistListQuery(cursor, limit),
      cancellationToken);

    return TypedResults.Ok(result.Value);
  }

  private static async Task<IResult> GetWishlistAsync(
    HttpContext httpContext,
    Guid wishlistId,
    IWishlistService wishlistService,
    ICurrentUserAccessor currentUserAccessor,
    CancellationToken cancellationToken)
  {
    var result = await wishlistService.GetByIdAsync(
      currentUserAccessor.GetRequiredUserId(),
      wishlistId,
      cancellationToken);

    if (result.ErrorCode == WishlistErrorCodes.NotFound)
    {
      return ApiProblem.NotFound(httpContext, "Wishlist not found.");
    }

    if (result.ErrorCode == WishlistErrorCodes.Forbidden)
    {
      return ApiProblem.Forbidden(httpContext, "Access denied.");
    }

    return TypedResults.Ok(result.Value);
  }

  private static async Task<IResult> PatchAsync(
    HttpContext httpContext,
    Guid wishlistId,
    UpdateWishlistRequestDto request,
    IWishlistService wishlistService,
    ICurrentUserAccessor currentUserAccessor,
    CancellationToken cancellationToken)
  {
    var result = await wishlistService.UpdateAsync(
      currentUserAccessor.GetRequiredUserId(),
      wishlistId,
      request,
      cancellationToken);

    return result.ErrorCode switch
    {
      null => TypedResults.Ok(result.Value),
      WishlistErrorCodes.NotFound => ApiProblem.NotFound(httpContext, "Wishlist not found."),
      WishlistErrorCodes.Forbidden => ApiProblem.Forbidden(httpContext, "Access denied."),
      WishlistErrorCodes.ThemeNotAccessible => ApiProblem.Validation(
        httpContext,
        ApiProblem.SingleFieldError("themeId", "themeId is not accessible."),
        "Validation failed."),
      _ => ApiProblem.Validation(
        httpContext,
        ApiProblem.RequestError("Validation failed."),
        "Validation failed.")
    };
  }

  private static async Task<IResult> DeleteAsync(
    HttpContext httpContext,
    Guid wishlistId,
    IWishlistService wishlistService,
    ICurrentUserAccessor currentUserAccessor,
    CancellationToken cancellationToken)
  {
    var result = await wishlistService.DeleteAsync(
      currentUserAccessor.GetRequiredUserId(),
      wishlistId,
      cancellationToken);

    if (result.ErrorCode == WishlistErrorCodes.NotFound)
    {
      return ApiProblem.NotFound(httpContext, "Wishlist not found.");
    }

    if (result.ErrorCode == WishlistErrorCodes.Forbidden)
    {
      return ApiProblem.Forbidden(httpContext, "Access denied.");
    }

    return TypedResults.NoContent();
  }
}
