namespace Wishlist.Api.Api.Errors;

public static class ApiProblemStatusCodeExtensions
{
  public static IApplicationBuilder UseApiProblemStatusCodePages(this IApplicationBuilder app)
  {
    return app.UseStatusCodePages(async context =>
    {
      var httpContext = context.HttpContext;
      var response = httpContext.Response;

      if (response.HasStarted)
      {
        return;
      }

      if (response.ContentLength is > 0 || !string.IsNullOrWhiteSpace(response.ContentType))
      {
        return;
      }

      IResult? result = response.StatusCode switch
      {
        StatusCodes.Status400BadRequest => ApiProblem.Validation(
          httpContext,
          ApiProblem.RequestError("Validation failed."),
          "Validation failed."),
        StatusCodes.Status401Unauthorized => ApiProblem.Unauthorized(httpContext, "Authentication is required."),
        StatusCodes.Status403Forbidden => ApiProblem.Forbidden(httpContext, "Access denied."),
        StatusCodes.Status404NotFound => ApiProblem.NotFound(httpContext, "Resource not found."),
        StatusCodes.Status429TooManyRequests => ApiProblem.TooManyRequests(httpContext, "Rate limit exceeded."),
        _ => null
      };

      if (result is not null)
      {
        await result.ExecuteAsync(httpContext);
      }
    });
  }
}
