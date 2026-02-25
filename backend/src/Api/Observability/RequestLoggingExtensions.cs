using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Routing;

namespace Wishlist.Api.Api.Observability;

public static class RequestLoggingExtensions
{
  public static IApplicationBuilder UseApiRequestLogging(this IApplicationBuilder app)
  {
    return app.UseMiddleware<ApiRequestLoggingMiddleware>();
  }
}

public sealed class ApiRequestLoggingMiddleware(RequestDelegate next, ILogger<ApiRequestLoggingMiddleware> logger)
{
  private readonly RequestDelegate _next = next;
  private readonly ILogger<ApiRequestLoggingMiddleware> _logger = logger;

  public async Task InvokeAsync(HttpContext httpContext)
  {
    var startedAt = Stopwatch.GetTimestamp();

    await _next(httpContext);

    var elapsedMs = Stopwatch.GetElapsedTime(startedAt).TotalMilliseconds;
    var method = httpContext.Request.Method;
    var path = ResolvePathForLogs(httpContext);
    var statusCode = httpContext.Response.StatusCode;
    var userId = ResolveUserId(httpContext.User);
    var correlationId = ResolveCorrelationId(httpContext);

    if (statusCode >= 500)
    {
      _logger.LogError(
        "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {DurationMs} ms (userId: {UserId}, correlationId: {CorrelationId})",
        method,
        path,
        statusCode,
        elapsedMs,
        userId,
        correlationId);
      return;
    }

    if (statusCode >= 400)
    {
      _logger.LogWarning(
        "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {DurationMs} ms (userId: {UserId}, correlationId: {CorrelationId})",
        method,
        path,
        statusCode,
        elapsedMs,
        userId,
        correlationId);
      return;
    }

    _logger.LogInformation(
      "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {DurationMs} ms (userId: {UserId}, correlationId: {CorrelationId})",
      method,
      path,
      statusCode,
      elapsedMs,
      userId,
      correlationId);
  }

  private static string ResolvePathForLogs(HttpContext httpContext)
  {
    var route = (httpContext.GetEndpoint() as RouteEndpoint)?.RoutePattern.RawText;
    if (!string.IsNullOrWhiteSpace(route))
    {
      return route;
    }

    return httpContext.Request.Path.Value ?? "/";
  }

  private static string ResolveUserId(ClaimsPrincipal principal)
  {
    var userId = principal.FindFirstValue(ClaimTypes.NameIdentifier)
      ?? principal.FindFirstValue("sub")
      ?? principal.FindFirstValue("userId");

    return string.IsNullOrWhiteSpace(userId) ? "anonymous" : userId;
  }

  private static string ResolveCorrelationId(HttpContext httpContext)
  {
    if (httpContext.Items.TryGetValue(CorrelationIdMiddleware.CorrelationIdItem, out var value)
      && value is string correlationId
      && !string.IsNullOrWhiteSpace(correlationId))
    {
      return correlationId;
    }

    return httpContext.TraceIdentifier;
  }
}
