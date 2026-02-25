using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Wishlist.Api.Infrastructure.Persistence;

namespace Wishlist.Api.Tests;

public sealed class PriorityMigrationTests
{
  [Fact]
  public async Task ConvertItemPriorityToDecimal_BackfillsLegacyValues()
  {
    await using var connection = new SqliteConnection("Data Source=:memory:");
    await connection.OpenAsync();

    var options = new DbContextOptionsBuilder<AppDbContext>()
      .UseSqlite(connection)
      .Options;

    var userId = Guid.NewGuid();
    var wishlistId = Guid.NewGuid();
    var now = DateTime.UtcNow;

    await using (var preMigrationContext = new AppDbContext(options))
    {
      var migrator = preMigrationContext.Database.GetService<IMigrator>();
      await migrator.MigrateAsync("20260224175933_AddThemeTokensV1");

      await preMigrationContext.Database.ExecuteSqlInterpolatedAsync($"""
        INSERT INTO users ("Id", "Email", "NormalizedEmail", "PasswordHash", "CreatedAtUtc")
        VALUES ({userId}, {"migration-owner@example.com"}, {"MIGRATION-OWNER@EXAMPLE.COM"}, {"hash"}, {now});
        """);

      await preMigrationContext.Database.ExecuteSqlInterpolatedAsync($"""
        INSERT INTO wishlists ("Id", "OwnerUserId", "Name", "CreatedAtUtc", "UpdatedAtUtc", "IsDeleted")
        VALUES ({wishlistId}, {userId}, {"Legacy Wishlist"}, {now}, {now}, {false});
        """);

      await preMigrationContext.Database.ExecuteSqlInterpolatedAsync($"""
        INSERT INTO wish_items ("WishlistId", "Name", "Priority", "CreatedAtUtc", "UpdatedAtUtc", "IsDeleted")
        VALUES ({wishlistId}, {"Legacy Item"}, {3}, {now}, {now}, {false});
        """);
    }

    await using (var postMigrationContext = new AppDbContext(options))
    {
      var migrator = postMigrationContext.Database.GetService<IMigrator>();
      await migrator.MigrateAsync();

      var priority = await postMigrationContext.WishItems
        .AsNoTracking()
        .Where(x => x.WishlistId == wishlistId)
        .Select(x => x.Priority)
        .SingleAsync();

      Assert.Equal(3072m, priority);
    }
  }
}
