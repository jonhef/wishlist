using Microsoft.AspNetCore.Mvc;

namespace Wishlist.Api.Api.Errors;

public static class ApiProblem
{
  public static IResult Validation(
    HttpContext httpContext,
    IDictionary<string, string[]> errors,
    string? detail = null)
  {
    var problem = Create(
      httpContext,
      StatusCodes.Status400BadRequest,
      "Validation error",
      "https://wishlist.local/problems/validation-error",
      detail);

    problem.Extensions["errors"] = errors;
    return TypedResults.Problem(problem);
  }

  public static IResult Unauthorized(HttpContext httpContext, string? detail = null)
  {
    return TypedResults.Problem(Create(
      httpContext,
      StatusCodes.Status401Unauthorized,
      "Unauthorized",
      "https://wishlist.local/problems/unauthorized",
      detail));
  }

  public static IResult Forbidden(HttpContext httpContext, string? detail = null)
  {
    return TypedResults.Problem(Create(
      httpContext,
      StatusCodes.Status403Forbidden,
      "Forbidden",
      "https://wishlist.local/problems/forbidden",
      detail));
  }

  public static IResult NotFound(HttpContext httpContext, string? detail = null)
  {
    return TypedResults.Problem(Create(
      httpContext,
      StatusCodes.Status404NotFound,
      "Not found",
      "https://wishlist.local/problems/not-found",
      detail));
  }

  public static IResult Conflict(HttpContext httpContext, string? detail = null)
  {
    return TypedResults.Problem(Create(
      httpContext,
      StatusCodes.Status409Conflict,
      "Conflict",
      "https://wishlist.local/problems/conflict",
      detail));
  }

  public static IResult TooManyRequests(HttpContext httpContext, string? detail = null)
  {
    return TypedResults.Problem(Create(
      httpContext,
      StatusCodes.Status429TooManyRequests,
      "Too many requests",
      "https://wishlist.local/problems/too-many-requests",
      detail));
  }

  public static IResult InternalServerError(HttpContext httpContext)
  {
    return TypedResults.Problem(Create(
      httpContext,
      StatusCodes.Status500InternalServerError,
      "Internal server error",
      "https://wishlist.local/problems/internal-server-error",
      "Unexpected error."));
  }

  public static Dictionary<string, string[]> SingleFieldError(string field, string message)
  {
    return new Dictionary<string, string[]>
    {
      [field] = new[] { message }
    };
  }

  public static Dictionary<string, string[]> RequestError(string message)
  {
    return SingleFieldError("request", message);
  }

  private static ProblemDetails Create(
    HttpContext httpContext,
    int status,
    string title,
    string type,
    string? detail)
  {
    var problem = new ProblemDetails
    {
      Status = status,
      Title = title,
      Type = type,
      Detail = detail,
      Instance = httpContext.Request.Path
    };

    problem.Extensions["traceId"] = httpContext.TraceIdentifier;
    return problem;
  }
}
