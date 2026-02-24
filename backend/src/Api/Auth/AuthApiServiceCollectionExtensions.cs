using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using Wishlist.Api.Features.Auth;

namespace Wishlist.Api.Api.Auth;

public static class AuthApiServiceCollectionExtensions
{
  public static IServiceCollection AddApiAuth(this IServiceCollection services, IConfiguration configuration)
  {
    var authOptions = configuration.GetSection(AuthOptions.SectionName).Get<AuthOptions>() ?? new AuthOptions();

    services.AddHttpContextAccessor();
    services.AddScoped<ICurrentUserAccessor, CurrentUserAccessor>();

    services.AddScoped<IAuthorizationHandler, OwnerAuthorizationHandler>();

    services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
      .AddJwtBearer(options =>
      {
        options.TokenValidationParameters = new TokenValidationParameters
        {
          ValidateIssuerSigningKey = true,
          IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(authOptions.AccessTokenSecret)),
          ValidateIssuer = true,
          ValidIssuer = authOptions.Issuer,
          ValidateAudience = true,
          ValidAudience = authOptions.Audience,
          ValidateLifetime = true,
          ClockSkew = TimeSpan.FromSeconds(5)
        };
      });

    services.AddAuthorization(options =>
    {
      options.AddPolicy(AuthorizationPolicies.OwnerOnly, policy =>
      {
        policy.RequireAuthenticatedUser();
        policy.AddRequirements(new OwnerRequirement());
      });
    });

    return services;
  }
}
