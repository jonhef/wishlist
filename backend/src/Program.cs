using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Serilog;
using System.Text.Json.Serialization;
using Wishlist.Api.Api.Auth;
using Wishlist.Api.Api.Errors;
using Wishlist.Api.Api.Observability;
using Wishlist.Api.Api.Public;
using Wishlist.Api.Api.Themes;
using Wishlist.Api.Api.Wishlists;
using Wishlist.Api.Features.Auth;
using Wishlist.Api.Features.Fx;
using Wishlist.Api.Features.Items;
using Wishlist.Api.Features.Sharing;
using Wishlist.Api.Features.Themes;
using Wishlist.Api.Features.Wishlists;
using Wishlist.Api.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddEnvironmentVariables();
builder.Host.UseSerilog((context, services, configuration) =>
{
  configuration
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Application", "wishlist-api");
});

var connectionString = builder.Configuration.GetConnectionString("WishlistDb")
  ?? throw new InvalidOperationException("Connection string 'WishlistDb' is not configured.");

builder.Services.AddDbContext<AppDbContext>(options =>
{
  options.UseNpgsql(connectionString, npgsqlOptions =>
  {
    npgsqlOptions.EnableRetryOnFailure(
      maxRetryCount: 5,
      maxRetryDelay: TimeSpan.FromSeconds(10),
      errorCodesToAdd: null);
  });

  if (builder.Environment.IsDevelopment())
  {
    options.EnableDetailedErrors();
    options.EnableSensitiveDataLogging(false);
    options.LogTo(
      Console.WriteLine,
      new[] { DbLoggerCategory.Database.Command.Name },
      LogLevel.Information);
  }
});
builder.Services.AddProblemDetails();
builder.Services.ConfigureHttpJsonOptions(options =>
{
  options.SerializerOptions.NumberHandling = JsonNumberHandling.AllowReadingFromString;
});
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddAuthModule(builder.Configuration);
builder.Services.AddApiAuth(builder.Configuration);
builder.Services.AddPublicApi();
builder.Services.AddFxModule(builder.Configuration);
builder.Services.AddWishlistModule();
builder.Services.AddThemeModule();
builder.Services.AddItemModule();
builder.Services.AddWishlistSharingModule();

var app = builder.Build();

await app.ApplyMigrationsIfNeededAsync();
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseApiRequestLogging();
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

app.MapGet("/health/ready", async (AppDbContext dbContext, CancellationToken cancellationToken) =>
{
  var canConnect = await dbContext.Database.CanConnectAsync(cancellationToken);
  if (!canConnect)
  {
    return Results.Problem(
      title: "Database unavailable",
      statusCode: StatusCodes.Status503ServiceUnavailable);
  }

  return Results.Ok(new
  {
    service = "backend-dotnet",
    status = "ready",
    database = "postgresql"
  });
});

app.Run();

public partial class Program;
