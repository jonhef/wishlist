using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Wishlist.Api.Domain.Entities;
using Wishlist.Api.Features.Sharing;
using Wishlist.Api.Infrastructure.Persistence;

namespace Wishlist.Api.Tests;

public sealed class WishlistShareServiceTests
{
  [Fact]
  public async Task RotateAndReadPublic_ReturnsWishlistWithVisibleItemsOnly()
  {
    await using var dbContext = CreateDbContext();
    var service = new WishlistShareService(dbContext);

    var owner = CreateUser("share-owner@example.com");
    var wishlist = CreateWishlist(owner.Id, "Share me", "public description");

    dbContext.Users.Add(owner);
    dbContext.Wishlists.Add(wishlist);
    dbContext.WishItems.AddRange(
      new WishItem
      {
        WishlistId = wishlist.Id,
        Name = "Visible",
        Url = "https://example.com/visible",
        PriceAmount = 99.90m,
        PriceCurrency = "USD",
        Priority = 3,
        Notes = "note",
        CreatedAtUtc = DateTime.UtcNow,
        UpdatedAtUtc = DateTime.UtcNow,
        IsDeleted = false
      },
      new WishItem
      {
        WishlistId = wishlist.Id,
        Name = "Deleted",
        Priority = 1,
        CreatedAtUtc = DateTime.UtcNow,
        UpdatedAtUtc = DateTime.UtcNow,
        IsDeleted = true,
        DeletedAtUtc = DateTime.UtcNow
      });
    await dbContext.SaveChangesAsync();

    var rotateResult = await service.RotateAsync(owner.Id, wishlist.Id, CancellationToken.None);
    Assert.True(rotateResult.IsSuccess);
    Assert.NotNull(rotateResult.Value);

    var publicResult = await service.GetPublicByTokenAsync(rotateResult.Value!.Token, CancellationToken.None);

    Assert.True(publicResult.IsSuccess);
    Assert.NotNull(publicResult.Value);
    Assert.Equal("Share me", publicResult.Value!.Title);
    Assert.Equal("public description", publicResult.Value.Description);
    Assert.Single(publicResult.Value.Items);
    Assert.Equal("Visible", publicResult.Value.Items[0].Name);
  }

  [Fact]
  public async Task RotateAsync_ForeignWishlist_ReturnsForbidden()
  {
    await using var dbContext = CreateDbContext();
    var service = new WishlistShareService(dbContext);

    var owner = CreateUser("share-owner2@example.com");
    var intruder = CreateUser("share-intruder2@example.com");
    var wishlist = CreateWishlist(owner.Id, "Private", null);

    dbContext.Users.AddRange(owner, intruder);
    dbContext.Wishlists.Add(wishlist);
    await dbContext.SaveChangesAsync();

    var result = await service.RotateAsync(intruder.Id, wishlist.Id, CancellationToken.None);

    Assert.False(result.IsSuccess);
    Assert.Equal(WishlistShareErrorCodes.Forbidden, result.ErrorCode);
  }

  [Fact]
  public async Task RotateAsync_RotatesToken_OldTokenBecomesInvalid()
  {
    await using var dbContext = CreateDbContext();
    var service = new WishlistShareService(dbContext);

    var owner = CreateUser("share-owner3@example.com");
    var wishlist = CreateWishlist(owner.Id, "Rotate", null);

    dbContext.Users.Add(owner);
    dbContext.Wishlists.Add(wishlist);
    await dbContext.SaveChangesAsync();

    var first = await service.RotateAsync(owner.Id, wishlist.Id, CancellationToken.None);
    var second = await service.RotateAsync(owner.Id, wishlist.Id, CancellationToken.None);

    Assert.True(first.IsSuccess);
    Assert.True(second.IsSuccess);
    Assert.NotEqual(first.Value!.Token, second.Value!.Token);

    var oldTokenResult = await service.GetPublicByTokenAsync(first.Value.Token, CancellationToken.None);
    var newTokenResult = await service.GetPublicByTokenAsync(second.Value.Token, CancellationToken.None);

    Assert.False(oldTokenResult.IsSuccess);
    Assert.Equal(WishlistShareErrorCodes.NotFound, oldTokenResult.ErrorCode);
    Assert.True(newTokenResult.IsSuccess);
  }

  [Fact]
  public async Task DisableAsync_MakesShareUnavailable()
  {
    await using var dbContext = CreateDbContext();
    var service = new WishlistShareService(dbContext);

    var owner = CreateUser("share-owner4@example.com");
    var wishlist = CreateWishlist(owner.Id, "Disable", null);

    dbContext.Users.Add(owner);
    dbContext.Wishlists.Add(wishlist);
    await dbContext.SaveChangesAsync();

    var rotate = await service.RotateAsync(owner.Id, wishlist.Id, CancellationToken.None);
    Assert.True(rotate.IsSuccess);

    var disable = await service.DisableAsync(owner.Id, wishlist.Id, CancellationToken.None);
    Assert.True(disable.IsSuccess);

    var publicResult = await service.GetPublicByTokenAsync(rotate.Value!.Token, CancellationToken.None);
    Assert.False(publicResult.IsSuccess);
    Assert.Equal(WishlistShareErrorCodes.NotFound, publicResult.ErrorCode);
  }

  private static AppDbContext CreateDbContext()
  {
    var options = new DbContextOptionsBuilder<AppDbContext>()
      .UseInMemoryDatabase($"share-tests-{Guid.NewGuid():N}")
      .Options;

    return new AppDbContext(options);
  }

  private static AppUser CreateUser(string email)
  {
    var user = new AppUser
    {
      Email = email,
      NormalizedEmail = email.ToUpperInvariant(),
      CreatedAtUtc = DateTime.UtcNow
    };

    user.PasswordHash = new PasswordHasher<AppUser>().HashPassword(user, "VeryStrongPass123");
    return user;
  }

  private static WishlistEntity CreateWishlist(Guid ownerId, string title, string? description)
  {
    var now = DateTime.UtcNow;

    return new WishlistEntity
    {
      OwnerUserId = ownerId,
      Title = title,
      Description = description,
      CreatedAtUtc = now,
      UpdatedAtUtc = now,
      IsDeleted = false
    };
  }
}
