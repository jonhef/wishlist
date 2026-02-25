using Microsoft.Extensions.Options;
using Wishlist.Api.Api.Errors;

namespace Wishlist.Api.Features.Auth;

public static class AuthEndpoints
{
  public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder endpoints)
  {
    var group = endpoints.MapGroup("/auth");

    group.MapPost("/register", RegisterAsync);
    group.MapPost("/login", LoginAsync);
    group.MapPost("/refresh", RefreshAsync);
    group.MapPost("/logout", LogoutAsync);

    return endpoints;
  }

  private static async Task<IResult> RegisterAsync(
    HttpContext httpContext,
    RegisterRequest request,
    IAuthService authService,
    CancellationToken cancellationToken)
  {
    var result = await authService.RegisterAsync(request, cancellationToken);

    if (result.IsSuccess && result.Value is not null)
    {
      return TypedResults.Created($"/auth/users/{result.Value.UserId}", result.Value);
    }

    return result.ErrorCode switch
    {
      "email_already_exists" => ApiProblem.Conflict(httpContext, "Email already exists."),
      "invalid_email" => ApiProblem.Validation(
        httpContext,
        ApiProblem.SingleFieldError("email", result.ErrorMessage ?? "Email format is invalid."),
        "Validation failed."),
      "invalid_password" => ApiProblem.Validation(
        httpContext,
        ApiProblem.SingleFieldError("password", result.ErrorMessage ?? "Password does not satisfy policy."),
        "Validation failed."),
      _ => ApiProblem.Validation(
        httpContext,
        ApiProblem.RequestError(result.ErrorMessage ?? "Validation failed."),
        "Validation failed.")
    };
  }

  private static async Task<IResult> LoginAsync(
    LoginRequest request,
    HttpContext httpContext,
    IAuthService authService,
    IOptions<AuthOptions> authOptions,
    CancellationToken cancellationToken)
  {
    var result = await authService.LoginAsync(
      request,
      httpContext.Connection.RemoteIpAddress?.ToString(),
      httpContext.Request.Headers.UserAgent.ToString(),
      cancellationToken);

    if (!result.IsSuccess || result.Value is null)
    {
      return ApiProblem.Unauthorized(httpContext, "Invalid credentials.");
    }

    var payload = ToTokensResponse(httpContext, authOptions.Value, result.Value);
    return TypedResults.Ok(payload);
  }

  private static async Task<IResult> RefreshAsync(
    RefreshRequest request,
    HttpContext httpContext,
    IAuthService authService,
    IOptions<AuthOptions> authOptions,
    CancellationToken cancellationToken)
  {
    var refreshToken = ResolveRefreshToken(request.RefreshToken, httpContext, authOptions.Value);

    var result = await authService.RefreshAsync(
      refreshToken,
      httpContext.Connection.RemoteIpAddress?.ToString(),
      httpContext.Request.Headers.UserAgent.ToString(),
      cancellationToken);

    if (!result.IsSuccess || result.Value is null)
    {
      return ApiProblem.Unauthorized(httpContext, "Refresh token is invalid.");
    }

    var payload = ToTokensResponse(httpContext, authOptions.Value, result.Value);
    return TypedResults.Ok(payload);
  }

  private static async Task<IResult> LogoutAsync(
    LogoutRequest request,
    HttpContext httpContext,
    IAuthService authService,
    IOptions<AuthOptions> authOptions,
    CancellationToken cancellationToken)
  {
    var options = authOptions.Value;
    var refreshToken = ResolveRefreshToken(request.RefreshToken, httpContext, options);

    await authService.LogoutAsync(refreshToken, cancellationToken);

    if (options.UseRefreshCookie)
    {
      httpContext.Response.Cookies.Delete(options.RefreshCookieName);
    }

    return TypedResults.NoContent();
  }

  private static AuthTokensResponse ToTokensResponse(HttpContext httpContext, AuthOptions options, TokenPair pair)
  {
    if (options.UseRefreshCookie)
    {
      httpContext.Response.Cookies.Append(
        options.RefreshCookieName,
        pair.RefreshToken,
        new CookieOptions
        {
          HttpOnly = true,
          Secure = true,
          SameSite = SameSiteMode.Lax,
          Expires = pair.RefreshTokenExpiresAtUtc,
          IsEssential = true,
          Path = "/"
        });

      return new AuthTokensResponse(
        pair.AccessToken,
        pair.AccessTokenExpiresAtUtc,
        null,
        pair.RefreshTokenExpiresAtUtc);
    }

    return new AuthTokensResponse(
      pair.AccessToken,
      pair.AccessTokenExpiresAtUtc,
      pair.RefreshToken,
      pair.RefreshTokenExpiresAtUtc);
  }

  private static string? ResolveRefreshToken(string? requestToken, HttpContext httpContext, AuthOptions options)
  {
    if (!string.IsNullOrWhiteSpace(requestToken))
    {
      return requestToken;
    }

    return httpContext.Request.Cookies.TryGetValue(options.RefreshCookieName, out var cookieToken)
      ? cookieToken
      : null;
  }
}
