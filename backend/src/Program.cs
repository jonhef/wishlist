using Microsoft.EntityFrameworkCore;
using Wishlist.Api.Api.Auth;
using Wishlist.Api.Api.Wishlists;
using Wishlist.Api.Features.Auth;
using Wishlist.Api.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddEnvironmentVariables();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
  ?? builder.Configuration["DB_CONNECTION_STRING"]
  ?? "Data Source=wishlist.dev.db";

builder.Services.AddDbContext<AppDbContext>(options =>
  options.UseSqlite(connectionString));
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddAuthModule(builder.Configuration);
builder.Services.AddApiAuth(builder.Configuration);

var app = builder.Build();

await app.ApplyMigrationsIfNeededAsync();
app.UseAuthentication();
app.UseAuthorization();

app.MapAuthEndpoints();
app.MapWishlistEndpoints();

app.MapGet("/health", () => Results.Ok(new
{
  service = "backend-dotnet",
  status = "ok",
  environment = app.Environment.EnvironmentName
}));

app.Run();
