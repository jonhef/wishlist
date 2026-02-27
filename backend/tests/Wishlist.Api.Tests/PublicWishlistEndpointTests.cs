using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Wishlist.Api.Domain.Entities;
using Wishlist.Api.Features.Fx;
using Wishlist.Api.Features.Sharing;
using Wishlist.Api.Infrastructure.Persistence;

namespace Wishlist.Api.Tests;

public sealed class PublicWishlistEndpointTests(PublicWishlistApiFactory factory) : IClassFixture<PublicWishlistApiFactory>
{
  private readonly PublicWishlistApiFactory _factory = factory;

  [Fact]
  public async Task GetPublicWishlist_SupportsBothSortModes_AndOmitsPriorityField()
  {
    var token = await _factory.SeedPublicWishlistAsync();
    using var client = _factory.CreateClient();

    var priorityResponse = await client.GetAsync($"/public/wishlists/{token}?sort=priority");
    var addedResponse = await client.GetAsync($"/public/wishlists/{token}?sort=added");

    Assert.Equal(HttpStatusCode.OK, priorityResponse.StatusCode);
    Assert.Equal(HttpStatusCode.OK, addedResponse.StatusCode);

    using var priorityJson = JsonDocument.Parse(await priorityResponse.Content.ReadAsStringAsync());
    using var addedJson = JsonDocument.Parse(await addedResponse.Content.ReadAsStringAsync());

    var priorityItems = priorityJson.RootElement.GetProperty("items");
    var addedItems = addedJson.RootElement.GetProperty("items");

    Assert.Equal(
      new[] { "P300-NewerId", "P300-OlderId", "P200-MidTime", "P100-Newest", "NoPrice", "Usd-Cheap" },
      GetNames(priorityItems));
    Assert.Equal(
      new[] { "Usd-Cheap", "NoPrice", "P100-Newest", "P200-MidTime", "P300-NewerId", "P300-OlderId" },
      GetNames(addedItems));

    Assert.Equal(priorityItems.GetArrayLength(), addedItems.GetArrayLength());

    foreach (var item in priorityItems.EnumerateArray())
    {
      Assert.False(item.TryGetProperty("priority", out _));
      Assert.True(item.TryGetProperty("id", out _));
      Assert.True(item.TryGetProperty("createdAt", out _));
    }
  }

  [Fact]
  public async Task GetPublicWishlist_UnknownSort_ReturnsBadRequest()
  {
    var token = await _factory.SeedPublicWishlistAsync();
    using var client = _factory.CreateClient();

    var response = await client.GetAsync($"/public/wishlists/{token}?sort=unknown");

    Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
  }

  [Fact]
  public async Task GetPublicWishlist_SortByPrice_SupportsAscDesc_AndKeepsUnknownAtEnd()
  {
    var token = await _factory.SeedPublicWishlistAsync();
    using var client = _factory.CreateClient();

    var ascResponse = await client.GetAsync($"/public/wishlists/{token}?sort=price&order=asc");
    var descResponse = await client.GetAsync($"/public/wishlists/{token}?sort=price&order=desc");

    Assert.Equal(HttpStatusCode.OK, ascResponse.StatusCode);
    Assert.Equal(HttpStatusCode.OK, descResponse.StatusCode);

    using var ascJson = JsonDocument.Parse(await ascResponse.Content.ReadAsStringAsync());
    using var descJson = JsonDocument.Parse(await descResponse.Content.ReadAsStringAsync());

    Assert.Equal(
      new[] { "Usd-Cheap", "P300-NewerId", "P200-MidTime", "P300-OlderId", "NoPrice", "P100-Newest" },
      GetNames(ascJson.RootElement.GetProperty("items")));
    Assert.Equal(
      new[] { "P300-OlderId", "P200-MidTime", "P300-NewerId", "Usd-Cheap", "NoPrice", "P100-Newest" },
      GetNames(descJson.RootElement.GetProperty("items")));
  }

  [Fact]
  public async Task GetPublicWishlist_SortByPrice_WithoutRates_ReturnsBadRequest()
  {
    var token = await _factory.SeedPublicWishlistAsync(seedFxRates: false);
    using var client = _factory.CreateClient();

    var response = await client.GetAsync($"/public/wishlists/{token}?sort=price&order=asc");

    Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
  }

  private static string[] GetNames(JsonElement items)
  {
    return items
      .EnumerateArray()
      .Select(item => item.GetProperty("name").GetString() ?? string.Empty)
      .ToArray();
  }
}

public sealed class PublicWishlistApiFactory : WebApplicationFactory<Program>
{
  private readonly string _dbName = $"public-endpoints-{Guid.NewGuid():N}";

  protected override void ConfigureWebHost(IWebHostBuilder builder)
  {
    builder.UseEnvironment("Testing");

    builder.ConfigureAppConfiguration((_, configBuilder) =>
    {
      configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
      {
        ["ConnectionStrings:WishlistDb"] = "Host=localhost;Database=wishlist_tests;Username=postgres;Password=postgres"
      });
    });

    builder.ConfigureServices(services =>
    {
      services.RemoveAll(typeof(DbContextOptions<AppDbContext>));
      services.RemoveAll(typeof(IDbContextOptionsConfiguration<AppDbContext>));
      services.RemoveAll(typeof(AppDbContext));
      services.AddDbContext<AppDbContext>(options =>
      {
        options.UseInMemoryDatabase(_dbName);
      });
    });
  }

  public async Task<string> SeedPublicWishlistAsync(bool seedFxRates = true)
  {
    using var scope = Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    await dbContext.Database.EnsureDeletedAsync();
    await dbContext.Database.EnsureCreatedAsync();

    var owner = CreateUser("public-endpoint-owner@example.com");
    var wishlist = CreateWishlist(owner.Id, "Public wishlist", "for endpoint tests");
    var baseTime = new DateTime(2026, 2, 1, 10, 0, 0, DateTimeKind.Utc);

    dbContext.Users.Add(owner);
    dbContext.Wishlists.Add(wishlist);
    dbContext.WishItems.AddRange(
      new WishItem
      {
        WishlistId = wishlist.Id,
        Name = "P300-OlderId",
        PriceAmount = 1999,
        PriceCurrency = "USD",
        Priority = 300m,
        CreatedAtUtc = baseTime.AddMinutes(1),
        UpdatedAtUtc = baseTime.AddMinutes(10),
        IsDeleted = false
      },
      new WishItem
      {
        WishlistId = wishlist.Id,
        Name = "P300-NewerId",
        PriceAmount = 1400,
        PriceCurrency = "EUR",
        Priority = 300m,
        CreatedAtUtc = baseTime.AddMinutes(1),
        UpdatedAtUtc = baseTime.AddMinutes(11),
        IsDeleted = false
      },
      new WishItem
      {
        WishlistId = wishlist.Id,
        Name = "P200-MidTime",
        PriceAmount = 150000,
        PriceCurrency = "RUB",
        Priority = 200m,
        CreatedAtUtc = baseTime.AddMinutes(2),
        UpdatedAtUtc = baseTime.AddMinutes(12),
        IsDeleted = false
      },
      new WishItem
      {
        WishlistId = wishlist.Id,
        Name = "P100-Newest",
        PriceAmount = 1200,
        PriceCurrency = "JPY",
        Priority = 100m,
        CreatedAtUtc = baseTime.AddMinutes(3),
        UpdatedAtUtc = baseTime.AddMinutes(13),
        IsDeleted = false
      },
      new WishItem
      {
        WishlistId = wishlist.Id,
        Name = "NoPrice",
        Priority = 50m,
        CreatedAtUtc = baseTime.AddMinutes(4),
        UpdatedAtUtc = baseTime.AddMinutes(14),
        IsDeleted = false
      },
      new WishItem
      {
        WishlistId = wishlist.Id,
        Name = "Usd-Cheap",
        PriceAmount = 599,
        PriceCurrency = "USD",
        Priority = 25m,
        CreatedAtUtc = baseTime.AddMinutes(5),
        UpdatedAtUtc = baseTime.AddMinutes(15),
        IsDeleted = false
      });

    if (seedFxRates)
    {
      dbContext.FxRates.AddRange(
        new FxRate
        {
          BaseCurrency = "EUR",
          QuoteCurrency = "EUR",
          RateToBase = 1m,
          AsOf = new DateOnly(2026, 2, 1),
          Source = "TEST",
          UpdatedAtUtc = baseTime
        },
        new FxRate
        {
          BaseCurrency = "EUR",
          QuoteCurrency = "USD",
          RateToBase = 0.90m,
          AsOf = new DateOnly(2026, 2, 1),
          Source = "TEST",
          UpdatedAtUtc = baseTime
        },
        new FxRate
        {
          BaseCurrency = "EUR",
          QuoteCurrency = "RUB",
          RateToBase = 0.01m,
          AsOf = new DateOnly(2026, 2, 1),
          Source = "TEST",
          UpdatedAtUtc = baseTime
        });
    }

    await dbContext.SaveChangesAsync();

    var shareService = new WishlistShareService(dbContext, new StubFxRatesService());
    var rotate = await shareService.RotateAsync(owner.Id, wishlist.Id, CancellationToken.None);

    if (!rotate.IsSuccess || rotate.Value is null)
    {
      throw new InvalidOperationException("Failed to seed public wishlist token for endpoint tests.");
    }

    return rotate.Value.Token;
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

  private sealed class StubFxRatesService : IFxRatesService
  {
    public Task<FxRatesSnapshot?> GetLatestSnapshotAsync(CancellationToken cancellationToken)
    {
      var asOf = new DateOnly(2026, 2, 26);
      var rates = new Dictionary<string, FxRateValue>(StringComparer.Ordinal)
      {
        [SupportedCurrencies.Eur] = new(1m, "TEST", asOf),
        [SupportedCurrencies.Usd] = new(0.92m, "TEST", asOf),
        [SupportedCurrencies.Rub] = new(0.0102m, "TEST", asOf),
        [SupportedCurrencies.Jpy] = new(0.0062m, "TEST", asOf)
      };

      return Task.FromResult<FxRatesSnapshot?>(new FxRatesSnapshot(asOf, rates));
    }
  }
}
