using System.Diagnostics;
using Serilog.Context;

namespace Wishlist.Api.Api.Observability;

public sealed class CorrelationIdMiddleware(RequestDelegate next)
{
  public const string CorrelationIdHeader = "X-Correlation-ID";
  public const string CorrelationIdItem = "CorrelationId";

  private readonly RequestDelegate _next = next;

  public async Task InvokeAsync(HttpContext httpContext)
  {
    var correlationId = ResolveCorrelationId(httpContext);

    httpContext.TraceIdentifier = correlationId;
    httpContext.Items[CorrelationIdItem] = correlationId;

    httpContext.Response.OnStarting(() =>
    {
      if (!httpContext.Response.Headers.ContainsKey(CorrelationIdHeader))
      {
        httpContext.Response.Headers[CorrelationIdHeader] = correlationId;
      }

      return Task.CompletedTask;
    });

    using (LogContext.PushProperty("CorrelationId", correlationId))
    using (LogContext.PushProperty("TraceId", correlationId))
    {
      await _next(httpContext);
    }
  }

  private static string ResolveCorrelationId(HttpContext httpContext)
  {
    var traceId = Activity.Current?.TraceId.ToString();
    if (!string.IsNullOrWhiteSpace(traceId))
    {
      return traceId;
    }

    if (httpContext.Request.Headers.TryGetValue(CorrelationIdHeader, out var incoming)
      && !string.IsNullOrWhiteSpace(incoming)
      && incoming.ToString().Length <= 64)
    {
      return incoming.ToString();
    }

    return httpContext.TraceIdentifier;
  }
}
