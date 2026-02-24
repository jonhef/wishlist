using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Wishlist.Api.Api.Auth;
using Wishlist.Api.Domain.Entities;
using Wishlist.Api.Infrastructure.Persistence;

namespace Wishlist.Api.Api.Wishlists;

public static class WishlistEndpoints
{
  public static IEndpointRouteBuilder MapWishlistEndpoints(this IEndpointRouteBuilder endpoints)
  {
    var group = endpoints.MapGroup("/api/wishlists").RequireAuthorization();

    group.MapPost("/", CreateWishlistAsync);
    group.MapGet("/{wishlistId:guid}", GetWishlistAsync);
    group.MapGet("/{wishlistId:guid}/items", GetWishlistItemsAsync);
    group.MapPost("/{wishlistId:guid}/items", CreateWishlistItemAsync);

    return endpoints;
  }

  private static async Task<IResult> CreateWishlistAsync(
    CreateWishlistRequest request,
    AppDbContext dbContext,
    ICurrentUserAccessor currentUserAccessor,
    CancellationToken cancellationToken)
  {
    if (string.IsNullOrWhiteSpace(request.Name))
    {
      return TypedResults.BadRequest(new { error = "Name is required" });
    }

    var wishlist = new WishlistEntity
    {
      OwnerUserId = currentUserAccessor.GetRequiredUserId(),
      Name = request.Name.Trim(),
      CreatedAtUtc = DateTime.UtcNow
    };

    dbContext.Wishlists.Add(wishlist);
    await dbContext.SaveChangesAsync(cancellationToken);

    return TypedResults.Created($"/api/wishlists/{wishlist.Id}",
      new WishlistResponse(wishlist.Id, wishlist.Name, wishlist.OwnerUserId, wishlist.CreatedAtUtc));
  }

  private static async Task<IResult> GetWishlistAsync(
    Guid wishlistId,
    AppDbContext dbContext,
    IAuthorizationService authorizationService,
    HttpContext httpContext,
    CancellationToken cancellationToken)
  {
    var wishlist = await dbContext.Wishlists
      .AsNoTracking()
      .FirstOrDefaultAsync(item => item.Id == wishlistId, cancellationToken);

    if (wishlist is null)
    {
      return TypedResults.NotFound();
    }

    var authResult = await authorizationService.AuthorizeAsync(
      httpContext.User,
      new OwnerResource(wishlist.OwnerUserId),
      AuthorizationPolicies.OwnerOnly);

    if (!authResult.Succeeded)
    {
      return TypedResults.Forbid();
    }

    return TypedResults.Ok(new WishlistResponse(
      wishlist.Id,
      wishlist.Name,
      wishlist.OwnerUserId,
      wishlist.CreatedAtUtc));
  }

  private static async Task<IResult> GetWishlistItemsAsync(
    Guid wishlistId,
    AppDbContext dbContext,
    IAuthorizationService authorizationService,
    HttpContext httpContext,
    CancellationToken cancellationToken)
  {
    var wishlist = await dbContext.Wishlists
      .AsNoTracking()
      .FirstOrDefaultAsync(item => item.Id == wishlistId, cancellationToken);

    if (wishlist is null)
    {
      return TypedResults.NotFound();
    }

    var authResult = await authorizationService.AuthorizeAsync(
      httpContext.User,
      new OwnerResource(wishlist.OwnerUserId),
      AuthorizationPolicies.OwnerOnly);

    if (!authResult.Succeeded)
    {
      return TypedResults.Forbid();
    }

    var items = await dbContext.WishItems
      .AsNoTracking()
      .Where(item => item.WishlistId == wishlistId)
      .OrderBy(item => item.Id)
      .Select(item => new WishlistItemResponse(item.Id, item.WishlistId, item.Title, item.CreatedAtUtc))
      .ToListAsync(cancellationToken);

    return TypedResults.Ok(items);
  }

  private static async Task<IResult> CreateWishlistItemAsync(
    Guid wishlistId,
    CreateWishlistItemRequest request,
    AppDbContext dbContext,
    IAuthorizationService authorizationService,
    HttpContext httpContext,
    CancellationToken cancellationToken)
  {
    if (string.IsNullOrWhiteSpace(request.Title))
    {
      return TypedResults.BadRequest(new { error = "Title is required" });
    }

    var wishlist = await dbContext.Wishlists
      .FirstOrDefaultAsync(item => item.Id == wishlistId, cancellationToken);

    if (wishlist is null)
    {
      return TypedResults.NotFound();
    }

    var authResult = await authorizationService.AuthorizeAsync(
      httpContext.User,
      new OwnerResource(wishlist.OwnerUserId),
      AuthorizationPolicies.OwnerOnly);

    if (!authResult.Succeeded)
    {
      return TypedResults.Forbid();
    }

    var wishItem = new WishItem
    {
      WishlistId = wishlistId,
      Title = request.Title.Trim(),
      CreatedAtUtc = DateTime.UtcNow
    };

    dbContext.WishItems.Add(wishItem);
    await dbContext.SaveChangesAsync(cancellationToken);

    return TypedResults.Created($"/api/wishlists/{wishlistId}/items/{wishItem.Id}",
      new WishlistItemResponse(wishItem.Id, wishItem.WishlistId, wishItem.Title, wishItem.CreatedAtUtc));
  }
}

public sealed record CreateWishlistRequest(string Name);

public sealed record WishlistResponse(Guid Id, string Name, Guid OwnerUserId, DateTime CreatedAtUtc);

public sealed record CreateWishlistItemRequest(string Title);

public sealed record WishlistItemResponse(int Id, Guid WishlistId, string Title, DateTime CreatedAtUtc);
