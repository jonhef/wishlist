using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Wishlist.Api.Api.Errors;

public sealed class ApiExceptionMiddleware(RequestDelegate next, ILogger<ApiExceptionMiddleware> logger)
{
  private readonly RequestDelegate _next = next;
  private readonly ILogger<ApiExceptionMiddleware> _logger = logger;

  public async Task InvokeAsync(HttpContext httpContext)
  {
    try
    {
      await _next(httpContext);
    }
    catch (OperationCanceledException) when (httpContext.RequestAborted.IsCancellationRequested)
    {
      throw;
    }
    catch (BadHttpRequestException ex)
    {
      _logger.LogWarning(ex, "Bad HTTP request.");

      if (httpContext.Response.HasStarted)
      {
        throw;
      }

      httpContext.Response.Clear();
      var result = ApiProblem.Validation(
        httpContext,
        ApiProblem.RequestError("Request is malformed."),
        "Request is malformed.");
      await result.ExecuteAsync(httpContext);
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Unhandled exception.");

      if (httpContext.Response.HasStarted)
      {
        throw;
      }

      httpContext.Response.Clear();
      await ApiProblem.InternalServerError(httpContext).ExecuteAsync(httpContext);
    }
  }
}
