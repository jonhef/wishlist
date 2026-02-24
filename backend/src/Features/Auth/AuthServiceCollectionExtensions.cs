using Microsoft.AspNetCore.Identity;
using Wishlist.Api.Domain.Entities;
using Wishlist.Api.Infrastructure.Persistence.Repositories;

namespace Wishlist.Api.Features.Auth;

public static class AuthServiceCollectionExtensions
{
  public static IServiceCollection AddAuthModule(this IServiceCollection services, IConfiguration configuration)
  {
    services.Configure<AuthOptions>(configuration.GetSection(AuthOptions.SectionName));

    services.AddScoped<IPasswordHasher<AppUser>, PasswordHasher<AppUser>>();
    services.AddScoped<IUserRepository, UserRepository>();
    services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
    services.AddScoped<ITokenService, TokenService>();
    services.AddScoped<IAuthService, AuthService>();

    return services;
  }
}
