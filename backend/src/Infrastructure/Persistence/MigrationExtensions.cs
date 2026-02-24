using Microsoft.EntityFrameworkCore;

namespace Wishlist.Api.Infrastructure.Persistence;

public static class MigrationExtensions
{
  public static async Task ApplyMigrationsIfNeededAsync(this WebApplication app)
  {
    var shouldApplyMigrations = app.Environment.IsDevelopment()
      && app.Configuration.GetValue("APPLY_MIGRATIONS_ON_STARTUP", false);

    if (!shouldApplyMigrations)
    {
      return;
    }

    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    await dbContext.Database.MigrateAsync();
  }
}
