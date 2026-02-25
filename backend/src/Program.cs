using Microsoft.EntityFrameworkCore;
using Wishlist.Api.Api.Auth;
using Wishlist.Api.Api.Errors;
using Wishlist.Api.Api.Public;
using Wishlist.Api.Api.Themes;
using Wishlist.Api.Api.Wishlists;
using Wishlist.Api.Features.Auth;
using Wishlist.Api.Features.Items;
using Wishlist.Api.Features.Sharing;
using Wishlist.Api.Features.Themes;
using Wishlist.Api.Features.Wishlists;
using Wishlist.Api.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddEnvironmentVariables();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
  ?? builder.Configuration["DB_CONNECTION_STRING"]
  ?? "Data Source=wishlist.dev.db";

builder.Services.AddDbContext<AppDbContext>(options =>
  options.UseSqlite(connectionString));
builder.Services.AddProblemDetails();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddAuthModule(builder.Configuration);
builder.Services.AddApiAuth(builder.Configuration);
builder.Services.AddPublicApi();
builder.Services.AddWishlistModule();
builder.Services.AddThemeModule();
builder.Services.AddItemModule();
builder.Services.AddWishlistSharingModule();

var app = builder.Build();

await app.ApplyMigrationsIfNeededAsync();
app.UseMiddleware<ApiExceptionMiddleware>();
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();
app.UseApiProblemStatusCodePages();

app.MapAuthEndpoints();
app.MapWishlistEndpoints();
app.MapThemeEndpoints();
app.MapPublicWishlistEndpoints();

app.MapGet("/health", () => Results.Ok(new
{
  service = "backend-dotnet",
  status = "ok",
  environment = app.Environment.EnvironmentName
}));

app.Run();
