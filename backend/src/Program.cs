using Microsoft.EntityFrameworkCore;
using Wishlist.Api.Domain.Entities;
using Wishlist.Api.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddEnvironmentVariables();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
  ?? builder.Configuration["DB_CONNECTION_STRING"]
  ?? "Data Source=wishlist.dev.db";

builder.Services.AddDbContext<AppDbContext>(options =>
  options.UseSqlite(connectionString));

var app = builder.Build();

await app.ApplyMigrationsIfNeededAsync();

app.MapGet("/health", () => Results.Ok(new
{
  service = "backend-dotnet",
  status = "ok",
  environment = app.Environment.EnvironmentName
}));

app.MapGet("/api/wishes", async (AppDbContext dbContext) =>
  await dbContext.WishItems
    .AsNoTracking()
    .OrderBy(item => item.Id)
    .ToListAsync());

app.MapPost("/api/wishes", async (AppDbContext dbContext, WishItemCreateRequest request) =>
{
  if (string.IsNullOrWhiteSpace(request.Title))
  {
    return Results.BadRequest(new { error = "Title is required" });
  }

  var item = new WishItem
  {
    Title = request.Title.Trim(),
    CreatedAtUtc = DateTime.UtcNow
  };

  dbContext.WishItems.Add(item);
  await dbContext.SaveChangesAsync();

  return Results.Created($"/api/wishes/{item.Id}", item);
});

app.Run();

public sealed record WishItemCreateRequest(string Title);
