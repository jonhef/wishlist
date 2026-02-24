using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Wishlist.Api.Domain.Entities;
using Wishlist.Api.Features.Items;
using Wishlist.Api.Infrastructure.Persistence;

namespace Wishlist.Api.Tests;

public sealed class ItemServiceTests
{
  [Fact]
  public async Task CreateAsync_CannotAddToForeignWishlist_ReturnsForbidden()
  {
    await using var dbContext = CreateDbContext();
    var service = new ItemService(dbContext, new FakeTimeProvider(DateTime.UtcNow));

    var owner = CreateUser("owner-item@example.com");
    var other = CreateUser("other-item@example.com");
    var foreignWishlist = CreateWishlist(other.Id, "Foreign");

    dbContext.Users.AddRange(owner, other);
    dbContext.Wishlists.Add(foreignWishlist);
    await dbContext.SaveChangesAsync();

    var result = await service.CreateAsync(
      owner.Id,
      foreignWishlist.Id,
      new CreateItemRequestDto("Laptop", null, null, null, 3, null),
      CancellationToken.None);

    Assert.False(result.IsSuccess);
    Assert.Equal(ItemErrorCodes.Forbidden, result.ErrorCode);
  }

  [Fact]
  public async Task CreateAsync_UrlWithoutScheme_NormalizesToHttps()
  {
    await using var dbContext = CreateDbContext();
    var service = new ItemService(dbContext, new FakeTimeProvider(DateTime.UtcNow));

    var owner = CreateUser("owner-url@example.com");
    var wishlist = CreateWishlist(owner.Id, "Main");

    dbContext.Users.Add(owner);
    dbContext.Wishlists.Add(wishlist);
    await dbContext.SaveChangesAsync();

    var result = await service.CreateAsync(
      owner.Id,
      wishlist.Id,
      new CreateItemRequestDto("Phone", "example.com/phone", 99.99m, "usd", 4, "buy soon"),
      CancellationToken.None);

    Assert.True(result.IsSuccess);
    Assert.NotNull(result.Value);
    Assert.Equal("https://example.com/phone", result.Value!.Url);
    Assert.Equal("USD", result.Value.PriceCurrency);
  }

  [Fact]
  public async Task CreateAsync_CurrencyWithoutAmount_ReturnsValidationFailed()
  {
    await using var dbContext = CreateDbContext();
    var service = new ItemService(dbContext, new FakeTimeProvider(DateTime.UtcNow));

    var owner = CreateUser("owner-currency@example.com");
    var wishlist = CreateWishlist(owner.Id, "Main");

    dbContext.Users.Add(owner);
    dbContext.Wishlists.Add(wishlist);
    await dbContext.SaveChangesAsync();

    var result = await service.CreateAsync(
      owner.Id,
      wishlist.Id,
      new CreateItemRequestDto("Table", null, null, "EUR", 2, null),
      CancellationToken.None);

    Assert.False(result.IsSuccess);
    Assert.Equal(ItemErrorCodes.ValidationFailed, result.ErrorCode);
  }

  [Fact]
  public async Task ListAsync_DoesNotReturnSoftDeletedItems()
  {
    await using var dbContext = CreateDbContext();
    var service = new ItemService(dbContext, new FakeTimeProvider(DateTime.UtcNow));

    var owner = CreateUser("owner-list@example.com");
    var wishlist = CreateWishlist(owner.Id, "Main");

    dbContext.Users.Add(owner);
    dbContext.Wishlists.Add(wishlist);
    dbContext.WishItems.AddRange(
      new WishItem { WishlistId = wishlist.Id, Name = "Visible", Priority = 1, CreatedAtUtc = DateTime.UtcNow, UpdatedAtUtc = DateTime.UtcNow, IsDeleted = false },
      new WishItem { WishlistId = wishlist.Id, Name = "Deleted", Priority = 2, CreatedAtUtc = DateTime.UtcNow, UpdatedAtUtc = DateTime.UtcNow, IsDeleted = true, DeletedAtUtc = DateTime.UtcNow });
    await dbContext.SaveChangesAsync();

    var result = await service.ListAsync(owner.Id, wishlist.Id, new ItemListQuery(null, 50), CancellationToken.None);

    Assert.True(result.IsSuccess);
    Assert.NotNull(result.Value);
    Assert.Single(result.Value!.Items);
    Assert.Equal("Visible", result.Value.Items[0].Name);
  }

  [Fact]
  public async Task UpdateAsync_EmptyPatch_ReturnsValidationFailed()
  {
    await using var dbContext = CreateDbContext();
    var service = new ItemService(dbContext, new FakeTimeProvider(DateTime.UtcNow));

    var owner = CreateUser("owner-patch@example.com");
    var wishlist = CreateWishlist(owner.Id, "Main");
    var item = new WishItem
    {
      WishlistId = wishlist.Id,
      Name = "Existing",
      Priority = 3,
      CreatedAtUtc = DateTime.UtcNow,
      UpdatedAtUtc = DateTime.UtcNow
    };

    dbContext.Users.Add(owner);
    dbContext.Wishlists.Add(wishlist);
    dbContext.WishItems.Add(item);
    await dbContext.SaveChangesAsync();

    var result = await service.UpdateAsync(
      owner.Id,
      wishlist.Id,
      item.Id,
      new UpdateItemRequestDto(null, null, null, null, null, null),
      CancellationToken.None);

    Assert.False(result.IsSuccess);
    Assert.Equal(ItemErrorCodes.ValidationFailed, result.ErrorCode);
  }

  private static AppDbContext CreateDbContext()
  {
    var options = new DbContextOptionsBuilder<AppDbContext>()
      .UseInMemoryDatabase($"item-tests-{Guid.NewGuid():N}")
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

  private static WishlistEntity CreateWishlist(Guid ownerId, string title)
  {
    var now = DateTime.UtcNow;

    return new WishlistEntity
    {
      OwnerUserId = ownerId,
      Title = title,
      CreatedAtUtc = now,
      UpdatedAtUtc = now,
      IsDeleted = false
    };
  }

  private sealed class FakeTimeProvider(DateTime utcNow) : TimeProvider
  {
    private readonly DateTime _utcNow = DateTime.SpecifyKind(utcNow, DateTimeKind.Utc);

    public override DateTimeOffset GetUtcNow() => new(_utcNow);
  }
}
