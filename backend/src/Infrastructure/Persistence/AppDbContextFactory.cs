using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Wishlist.Api.Infrastructure.Persistence;

public sealed class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
  public AppDbContext CreateDbContext(string[] args)
  {
    var connectionString =
      Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
      ?? Environment.GetEnvironmentVariable("DB_CONNECTION_STRING")
      ?? "Data Source=wishlist.dev.db";

    var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
    optionsBuilder.UseSqlite(connectionString);

    return new AppDbContext(optionsBuilder.Options);
  }
}
