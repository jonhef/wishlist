using Wishlist.Api.Api.Auth;
using Wishlist.Api.Api.Errors;
using Wishlist.Api.Features.Items;

namespace Wishlist.Api.Api.Wishlists;

public static class WishlistItemEndpoints
{
  public static RouteGroupBuilder MapWishlistItemCrud(this RouteGroupBuilder group)
  {
    group.MapPost("/{wishlistId:guid}/items", CreateAsync);
    group.MapGet("/{wishlistId:guid}/items", ListAsync);
    group.MapPatch("/{wishlistId:guid}/items/{itemId:int}", PatchAsync);
    group.MapDelete("/{wishlistId:guid}/items/{itemId:int}", DeleteAsync);

    return group;
  }

  private static async Task<IResult> CreateAsync(
    HttpContext httpContext,
    Guid wishlistId,
    CreateItemRequestDto request,
    IItemService itemService,
    ICurrentUserAccessor currentUserAccessor,
    CancellationToken cancellationToken)
  {
    var result = await itemService.CreateAsync(
      currentUserAccessor.GetRequiredUserId(),
      wishlistId,
      request,
      cancellationToken);

    return result.ErrorCode switch
    {
      null => TypedResults.Created($"/wishlists/{wishlistId}/items/{result.Value!.Id}", result.Value),
      ItemErrorCodes.NotFound => ApiProblem.NotFound(httpContext, "Wishlist or item not found."),
      ItemErrorCodes.Forbidden => ApiProblem.Forbidden(httpContext, "Access denied."),
      _ => ApiProblem.Validation(
        httpContext,
        ApiProblem.RequestError("Validation failed."),
        "Validation failed.")
    };
  }

  private static async Task<IResult> ListAsync(
    HttpContext httpContext,
    Guid wishlistId,
    string? cursor,
    int? limit,
    IItemService itemService,
    ICurrentUserAccessor currentUserAccessor,
    CancellationToken cancellationToken)
  {
    var result = await itemService.ListAsync(
      currentUserAccessor.GetRequiredUserId(),
      wishlistId,
      new ItemListQuery(cursor, limit),
      cancellationToken);

    return result.ErrorCode switch
    {
      null => TypedResults.Ok(result.Value),
      ItemErrorCodes.NotFound => ApiProblem.NotFound(httpContext, "Wishlist not found."),
      ItemErrorCodes.Forbidden => ApiProblem.Forbidden(httpContext, "Access denied."),
      _ => ApiProblem.Validation(
        httpContext,
        ApiProblem.RequestError("Validation failed."),
        "Validation failed.")
    };
  }

  private static async Task<IResult> PatchAsync(
    HttpContext httpContext,
    Guid wishlistId,
    int itemId,
    UpdateItemRequestDto request,
    IItemService itemService,
    ICurrentUserAccessor currentUserAccessor,
    CancellationToken cancellationToken)
  {
    var result = await itemService.UpdateAsync(
      currentUserAccessor.GetRequiredUserId(),
      wishlistId,
      itemId,
      request,
      cancellationToken);

    return result.ErrorCode switch
    {
      null => TypedResults.Ok(result.Value),
      ItemErrorCodes.NotFound => ApiProblem.NotFound(httpContext, "Item not found."),
      ItemErrorCodes.Forbidden => ApiProblem.Forbidden(httpContext, "Access denied."),
      _ => ApiProblem.Validation(
        httpContext,
        ApiProblem.RequestError("Validation failed."),
        "Validation failed.")
    };
  }

  private static async Task<IResult> DeleteAsync(
    HttpContext httpContext,
    Guid wishlistId,
    int itemId,
    IItemService itemService,
    ICurrentUserAccessor currentUserAccessor,
    CancellationToken cancellationToken)
  {
    var result = await itemService.DeleteAsync(
      currentUserAccessor.GetRequiredUserId(),
      wishlistId,
      itemId,
      cancellationToken);

    return result.ErrorCode switch
    {
      null => TypedResults.NoContent(),
      ItemErrorCodes.NotFound => ApiProblem.NotFound(httpContext, "Item not found."),
      ItemErrorCodes.Forbidden => ApiProblem.Forbidden(httpContext, "Access denied."),
      _ => ApiProblem.Validation(
        httpContext,
        ApiProblem.RequestError("Validation failed."),
        "Validation failed.")
    };
  }
}
