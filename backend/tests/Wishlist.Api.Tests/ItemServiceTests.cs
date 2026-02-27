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
      new CreateItemRequestDto("Phone", "example.com/phone", 9999, "usd", 4, "buy soon"),
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
  public async Task ListAsync_LimitAboveMax_ReturnsTwoPagesWithMax50()
  {
    await using var dbContext = CreateDbContext();
    var baseTime = DateTime.UtcNow;
    var service = new ItemService(dbContext, new FakeTimeProvider(baseTime));

    var owner = CreateUser("owner-items-pagination@example.com");
    var wishlist = CreateWishlist(owner.Id, "Main");

    dbContext.Users.Add(owner);
    dbContext.Wishlists.Add(wishlist);

    for (var i = 0; i < 55; i++)
    {
      dbContext.WishItems.Add(new WishItem
      {
        WishlistId = wishlist.Id,
        Name = $"Item-{i}",
        Priority = i % 6,
        CreatedAtUtc = baseTime.AddSeconds(i),
        UpdatedAtUtc = baseTime.AddSeconds(i),
        IsDeleted = false
      });
    }

    await dbContext.SaveChangesAsync();

    var firstPage = await service.ListAsync(owner.Id, wishlist.Id, new ItemListQuery(null, 500), CancellationToken.None);
    Assert.True(firstPage.IsSuccess);
    Assert.NotNull(firstPage.Value);
    Assert.Equal(50, firstPage.Value!.Items.Count);
    Assert.NotNull(firstPage.Value.NextCursor);

    var secondPage = await service.ListAsync(
      owner.Id,
      wishlist.Id,
      new ItemListQuery(firstPage.Value.NextCursor, 500),
      CancellationToken.None);

    Assert.True(secondPage.IsSuccess);
    Assert.NotNull(secondPage.Value);
    Assert.Equal(5, secondPage.Value!.Items.Count);
    Assert.Null(secondPage.Value.NextCursor);

    var allIds = firstPage.Value.Items.Select(x => x.Id).Concat(secondPage.Value.Items.Select(x => x.Id)).ToList();
    Assert.Equal(55, allIds.Distinct().Count());
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

  [Fact]
  public async Task ListAsync_SortsByPriorityThenCreatedAtDesc()
  {
    await using var dbContext = CreateDbContext();
    var service = new ItemService(dbContext, new FakeTimeProvider(DateTime.UtcNow));

    var owner = CreateUser("owner-sorting@example.com");
    var wishlist = CreateWishlist(owner.Id, "Main");
    var baseTime = DateTime.UtcNow;

    dbContext.Users.Add(owner);
    dbContext.Wishlists.Add(wishlist);
    dbContext.WishItems.AddRange(
      new WishItem
      {
        WishlistId = wishlist.Id,
        Name = "Low",
        Priority = 100m,
        CreatedAtUtc = baseTime.AddMinutes(3),
        UpdatedAtUtc = baseTime.AddMinutes(10),
        IsDeleted = false
      },
      new WishItem
      {
        WishlistId = wishlist.Id,
        Name = "High-Older",
        Priority = 200m,
        CreatedAtUtc = baseTime.AddMinutes(1),
        UpdatedAtUtc = baseTime.AddMinutes(11),
        IsDeleted = false
      },
      new WishItem
      {
        WishlistId = wishlist.Id,
        Name = "High-Newer",
        Priority = 200m,
        CreatedAtUtc = baseTime.AddMinutes(2),
        UpdatedAtUtc = baseTime.AddMinutes(9),
        IsDeleted = false
      });
    await dbContext.SaveChangesAsync();

    var result = await service.ListAsync(owner.Id, wishlist.Id, new ItemListQuery(null, 50), CancellationToken.None);

    Assert.True(result.IsSuccess);
    Assert.NotNull(result.Value);
    Assert.Equal(
      new[] { "High-Newer", "High-Older", "Low" },
      result.Value!.Items.Select(x => x.Name).ToArray());
  }

  [Fact]
  public async Task CreateAsync_WithoutPriority_PlacesItemAtBottom()
  {
    await using var dbContext = CreateDbContext();
    var service = new ItemService(dbContext, new FakeTimeProvider(DateTime.UtcNow));

    var owner = CreateUser("owner-default-priority@example.com");
    var wishlist = CreateWishlist(owner.Id, "Main");
    var now = DateTime.UtcNow;

    dbContext.Users.Add(owner);
    dbContext.Wishlists.Add(wishlist);
    dbContext.WishItems.AddRange(
      new WishItem
      {
        WishlistId = wishlist.Id,
        Name = "Top",
        Priority = 4096m,
        CreatedAtUtc = now,
        UpdatedAtUtc = now,
        IsDeleted = false
      },
      new WishItem
      {
        WishlistId = wishlist.Id,
        Name = "Bottom",
        Priority = 1024m,
        CreatedAtUtc = now.AddMinutes(1),
        UpdatedAtUtc = now.AddMinutes(1),
        IsDeleted = false
      });
    await dbContext.SaveChangesAsync();

    var created = await service.CreateAsync(
      owner.Id,
      wishlist.Id,
      new CreateItemRequestDto("Auto-priority", null, null, null, null, null),
      CancellationToken.None);

    Assert.True(created.IsSuccess);
    Assert.NotNull(created.Value);
    Assert.Equal(0m, created.Value!.Priority);
  }

  [Fact]
  public async Task RebalanceAsync_ReassignsPrioritiesAndPreservesOrder()
  {
    await using var dbContext = CreateDbContext();
    var service = new ItemService(dbContext, new FakeTimeProvider(DateTime.UtcNow));

    var owner = CreateUser("owner-rebalance@example.com");
    var wishlist = CreateWishlist(owner.Id, "Main");
    var baseTime = DateTime.UtcNow;

    dbContext.Users.Add(owner);
    dbContext.Wishlists.Add(wishlist);
    dbContext.WishItems.AddRange(
      new WishItem
      {
        WishlistId = wishlist.Id,
        Name = "Top-Newer",
        Priority = 10.000000001m,
        CreatedAtUtc = baseTime.AddMinutes(2),
        UpdatedAtUtc = baseTime.AddMinutes(2),
        IsDeleted = false
      },
      new WishItem
      {
        WishlistId = wishlist.Id,
        Name = "Top-Older",
        Priority = 10.000000001m,
        CreatedAtUtc = baseTime.AddMinutes(1),
        UpdatedAtUtc = baseTime.AddMinutes(1),
        IsDeleted = false
      },
      new WishItem
      {
        WishlistId = wishlist.Id,
        Name = "Bottom",
        Priority = 9.999999999m,
        CreatedAtUtc = baseTime.AddMinutes(3),
        UpdatedAtUtc = baseTime.AddMinutes(3),
        IsDeleted = false
      });
    await dbContext.SaveChangesAsync();

    var rebalance = await service.RebalanceAsync(owner.Id, wishlist.Id, CancellationToken.None);
    Assert.True(rebalance.IsSuccess);
    Assert.NotNull(rebalance.Value);
    Assert.Equal(3, rebalance.Value!.RebalancedCount);

    var listed = await service.ListAsync(owner.Id, wishlist.Id, new ItemListQuery(null, 50), CancellationToken.None);
    Assert.True(listed.IsSuccess);
    Assert.NotNull(listed.Value);
    Assert.Equal(
      new[] { "Top-Newer", "Top-Older", "Bottom" },
      listed.Value!.Items.Select(x => x.Name).ToArray());
    Assert.Equal(
      new[] { 3072m, 2048m, 1024m },
      listed.Value.Items.Select(x => x.Priority).ToArray());
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
