using Wishlist.Api.Api.Auth;
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
  }

  private static async Task<IResult> CreateAsync(
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
      WishlistErrorCodes.ThemeNotAccessible => TypedResults.BadRequest(new { error = "themeId is not accessible." }),
      _ => TypedResults.BadRequest(new { error = "Validation failed." })
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
      return TypedResults.NotFound();
    }

    if (result.ErrorCode == WishlistErrorCodes.Forbidden)
    {
      return TypedResults.Forbid();
    }

    return TypedResults.Ok(result.Value);
  }

  private static async Task<IResult> PatchAsync(
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
      WishlistErrorCodes.NotFound => TypedResults.NotFound(),
      WishlistErrorCodes.Forbidden => TypedResults.Forbid(),
      WishlistErrorCodes.ThemeNotAccessible => TypedResults.BadRequest(new { error = "themeId is not accessible." }),
      _ => TypedResults.BadRequest(new { error = "Validation failed." })
    };
  }

  private static async Task<IResult> DeleteAsync(
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
      return TypedResults.NotFound();
    }

    if (result.ErrorCode == WishlistErrorCodes.Forbidden)
    {
      return TypedResults.Forbid();
    }

    return TypedResults.NoContent();
  }
}
