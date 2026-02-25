using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Wishlist.Api.Domain.Entities;
using Wishlist.Api.Features.Wishlists;
using Wishlist.Api.Infrastructure.Persistence;

namespace Wishlist.Api.Tests;

public sealed class WishlistServiceTests
{
  [Fact]
  public async Task ListAsync_ReturnsOnlyOwnerWishlists()
  {
    await using var dbContext = CreateDbContext();
    var timeProvider = new FakeTimeProvider(DateTime.UtcNow);
    var service = new WishlistService(dbContext, timeProvider);

    var owner = CreateUser("owner@example.com");
    var intruder = CreateUser("intruder@example.com");

    dbContext.Users.AddRange(owner, intruder);

    var wishlist1 = CreateWishlist(owner.Id, "Owner 1", timeProvider.GetUtcNow().UtcDateTime.AddMinutes(-2));
    var wishlist2 = CreateWishlist(owner.Id, "Owner 2", timeProvider.GetUtcNow().UtcDateTime.AddMinutes(-1));
    var foreignWishlist = CreateWishlist(intruder.Id, "Foreign", timeProvider.GetUtcNow().UtcDateTime);

    dbContext.Wishlists.AddRange(wishlist1, wishlist2, foreignWishlist);
    dbContext.WishItems.AddRange(
      new WishItem { WishlistId = wishlist1.Id, Name = "A", Priority = 1, CreatedAtUtc = DateTime.UtcNow, UpdatedAtUtc = DateTime.UtcNow },
      new WishItem { WishlistId = wishlist1.Id, Name = "B", Priority = 1, CreatedAtUtc = DateTime.UtcNow, UpdatedAtUtc = DateTime.UtcNow },
      new WishItem { WishlistId = wishlist2.Id, Name = "C", Priority = 1, CreatedAtUtc = DateTime.UtcNow, UpdatedAtUtc = DateTime.UtcNow },
      new WishItem { WishlistId = foreignWishlist.Id, Name = "D", Priority = 1, CreatedAtUtc = DateTime.UtcNow, UpdatedAtUtc = DateTime.UtcNow });

    await dbContext.SaveChangesAsync();

    var result = await service.ListAsync(owner.Id, new WishlistListQuery(null, 50), CancellationToken.None);

    Assert.True(result.IsSuccess);
    Assert.NotNull(result.Value);
    Assert.Equal(2, result.Value!.Items.Count);
    Assert.All(result.Value.Items, item => Assert.Contains(item.Id, new[] { wishlist1.Id, wishlist2.Id }));
    Assert.Equal(2, result.Value.Items.First(item => item.Id == wishlist1.Id).ItemsCount);
  }

  [Fact]
  public async Task ListAsync_LimitAboveMax_ReturnsTwoPagesWithMax50()
  {
    await using var dbContext = CreateDbContext();
    var now = DateTime.UtcNow;
    var service = new WishlistService(dbContext, new FakeTimeProvider(now));
    var owner = CreateUser("owner-pagination@example.com");

    dbContext.Users.Add(owner);

    for (var i = 0; i < 55; i++)
    {
      dbContext.Wishlists.Add(CreateWishlist(owner.Id, $"WL-{i}", now.AddMinutes(i)));
    }

    await dbContext.SaveChangesAsync();

    var firstPage = await service.ListAsync(owner.Id, new WishlistListQuery(null, 500), CancellationToken.None);
    Assert.True(firstPage.IsSuccess);
    Assert.NotNull(firstPage.Value);
    Assert.Equal(50, firstPage.Value!.Items.Count);
    Assert.NotNull(firstPage.Value.NextCursor);

    var secondPage = await service.ListAsync(
      owner.Id,
      new WishlistListQuery(firstPage.Value.NextCursor, 500),
      CancellationToken.None);

    Assert.True(secondPage.IsSuccess);
    Assert.NotNull(secondPage.Value);
    Assert.Equal(5, secondPage.Value!.Items.Count);
    Assert.Null(secondPage.Value.NextCursor);

    var allIds = firstPage.Value.Items.Select(x => x.Id).Concat(secondPage.Value.Items.Select(x => x.Id)).ToList();
    Assert.Equal(55, allIds.Distinct().Count());
  }

  [Fact]
  public async Task UpdateAsync_ChangesUpdatedAt()
  {
    var now = new DateTime(2026, 2, 24, 10, 0, 0, DateTimeKind.Utc);
    await using var dbContext = CreateDbContext();
    var timeProvider = new FakeTimeProvider(now);
    var service = new WishlistService(dbContext, timeProvider);

    var owner = CreateUser("owner2@example.com");
    var wishlist = CreateWishlist(owner.Id, "Before", now);

    dbContext.Users.Add(owner);
    dbContext.Wishlists.Add(wishlist);
    await dbContext.SaveChangesAsync();

    timeProvider.Set(now.AddMinutes(5));

    var result = await service.UpdateAsync(
      owner.Id,
      wishlist.Id,
      new UpdateWishlistRequestDto("After", null, null),
      CancellationToken.None);

    Assert.True(result.IsSuccess);
    Assert.NotNull(result.Value);
    Assert.Equal("After", result.Value!.Title);
    Assert.True(result.Value.UpdatedAt > now);

    var updatedEntity = await dbContext.Wishlists.FirstAsync(item => item.Id == wishlist.Id);
    Assert.Equal(result.Value.UpdatedAt, updatedEntity.UpdatedAtUtc);
  }

  [Fact]
  public async Task UpdateAsync_EmptyPatch_ReturnsValidationFailed()
  {
    await using var dbContext = CreateDbContext();
    var service = new WishlistService(dbContext, new FakeTimeProvider(DateTime.UtcNow));

    var owner = CreateUser("owner3@example.com");
    var wishlist = CreateWishlist(owner.Id, "Before", DateTime.UtcNow);

    dbContext.Users.Add(owner);
    dbContext.Wishlists.Add(wishlist);
    await dbContext.SaveChangesAsync();

    var result = await service.UpdateAsync(
      owner.Id,
      wishlist.Id,
      new UpdateWishlistRequestDto(null, null, null),
      CancellationToken.None);

    Assert.False(result.IsSuccess);
    Assert.Equal(WishlistErrorCodes.ValidationFailed, result.ErrorCode);
  }

  [Fact]
  public async Task DeleteAsync_PerformsSoftDelete()
  {
    await using var dbContext = CreateDbContext();
    var now = DateTime.UtcNow;
    var timeProvider = new FakeTimeProvider(now);
    var service = new WishlistService(dbContext, timeProvider);

    var owner = CreateUser("owner4@example.com");
    var wishlist = CreateWishlist(owner.Id, "To Delete", now);

    dbContext.Users.Add(owner);
    dbContext.Wishlists.Add(wishlist);
    await dbContext.SaveChangesAsync();

    timeProvider.Set(now.AddMinutes(1));

    var deleteResult = await service.DeleteAsync(owner.Id, wishlist.Id, CancellationToken.None);
    Assert.True(deleteResult.IsSuccess);

    var deleted = await dbContext.Wishlists.FirstAsync(item => item.Id == wishlist.Id);
    Assert.True(deleted.IsDeleted);
    Assert.NotNull(deleted.DeletedAtUtc);

    var listResult = await service.ListAsync(owner.Id, new WishlistListQuery(null, 50), CancellationToken.None);
    Assert.True(listResult.IsSuccess);
    Assert.Empty(listResult.Value!.Items);
  }

  [Fact]
  public async Task CreateAsync_WithForeignTheme_ReturnsThemeNotAccessible()
  {
    await using var dbContext = CreateDbContext();
    var service = new WishlistService(dbContext, new FakeTimeProvider(DateTime.UtcNow));

    var owner = CreateUser("owner5@example.com");
    var otherUser = CreateUser("other5@example.com");
    var foreignTheme = new ThemeEntity
    {
      OwnerUserId = otherUser.Id,
      Name = "Foreign",
      CreatedAtUtc = DateTime.UtcNow
    };

    dbContext.Users.AddRange(owner, otherUser);
    dbContext.Themes.Add(foreignTheme);
    await dbContext.SaveChangesAsync();

    var result = await service.CreateAsync(
      owner.Id,
      new CreateWishlistRequestDto("Wishlist", null, foreignTheme.Id),
      CancellationToken.None);

    Assert.False(result.IsSuccess);
    Assert.Equal(WishlistErrorCodes.ThemeNotAccessible, result.ErrorCode);
  }

  private static AppDbContext CreateDbContext()
  {
    var options = new DbContextOptionsBuilder<AppDbContext>()
      .UseInMemoryDatabase(databaseName: $"wishlist-tests-{Guid.NewGuid():N}")
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

  private static WishlistEntity CreateWishlist(Guid ownerId, string title, DateTime at)
  {
    return new WishlistEntity
    {
      OwnerUserId = ownerId,
      Title = title,
      CreatedAtUtc = at,
      UpdatedAtUtc = at,
      IsDeleted = false
    };
  }

  private sealed class FakeTimeProvider(DateTime utcNow) : TimeProvider
  {
    private DateTime _utcNow = DateTime.SpecifyKind(utcNow, DateTimeKind.Utc);

    public override DateTimeOffset GetUtcNow() => new(_utcNow);

    public void Set(DateTime utcNow)
    {
      _utcNow = DateTime.SpecifyKind(utcNow, DateTimeKind.Utc);
    }
  }
}
