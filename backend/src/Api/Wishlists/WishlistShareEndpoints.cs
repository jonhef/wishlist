using Wishlist.Api.Api.Auth;
using Wishlist.Api.Api.Errors;
using Wishlist.Api.Features.Sharing;

namespace Wishlist.Api.Api.Wishlists;

public static class WishlistShareEndpoints
{
  public static RouteGroupBuilder MapWishlistShareManagement(this RouteGroupBuilder group)
  {
    group.MapPost("/{wishlistId:guid}/share", RotateAsync);
    group.MapDelete("/{wishlistId:guid}/share", DisableAsync);

    return group;
  }

  private static async Task<IResult> RotateAsync(
    Guid wishlistId,
    HttpContext httpContext,
    IWishlistShareService wishlistShareService,
    ICurrentUserAccessor currentUserAccessor,
    CancellationToken cancellationToken)
  {
    var result = await wishlistShareService.RotateAsync(
      currentUserAccessor.GetRequiredUserId(),
      wishlistId,
      cancellationToken);

    return result.ErrorCode switch
    {
      null => TypedResults.Ok(new
      {
        publicUrl = BuildPublicUrl(httpContext, result.Value!.Token)
      }),
      WishlistShareErrorCodes.NotFound => ApiProblem.NotFound(httpContext, "Wishlist not found."),
      WishlistShareErrorCodes.Forbidden => ApiProblem.Forbidden(httpContext, "Access denied."),
      _ => ApiProblem.Validation(
        httpContext,
        ApiProblem.RequestError("Validation failed."),
        "Validation failed.")
    };
  }

  private static async Task<IResult> DisableAsync(
    HttpContext httpContext,
    Guid wishlistId,
    IWishlistShareService wishlistShareService,
    ICurrentUserAccessor currentUserAccessor,
    CancellationToken cancellationToken)
  {
    var result = await wishlistShareService.DisableAsync(
      currentUserAccessor.GetRequiredUserId(),
      wishlistId,
      cancellationToken);

    return result.ErrorCode switch
    {
      null => TypedResults.NoContent(),
      WishlistShareErrorCodes.NotFound => ApiProblem.NotFound(httpContext, "Wishlist not found."),
      WishlistShareErrorCodes.Forbidden => ApiProblem.Forbidden(httpContext, "Access denied."),
      _ => ApiProblem.Validation(
        httpContext,
        ApiProblem.RequestError("Validation failed."),
        "Validation failed.")
    };
  }

  private static string BuildPublicUrl(HttpContext httpContext, string token)
  {
    var request = httpContext.Request;
    return $"{request.Scheme}://{request.Host}/public/wishlists/{token}";
  }
}
