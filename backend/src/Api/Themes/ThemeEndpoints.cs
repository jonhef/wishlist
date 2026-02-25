using Wishlist.Api.Api.Auth;
using Wishlist.Api.Api.Errors;
using Wishlist.Api.Features.Themes;

namespace Wishlist.Api.Api.Themes;

public static class ThemeEndpoints
{
  public static IEndpointRouteBuilder MapThemeEndpoints(this IEndpointRouteBuilder endpoints)
  {
    MapCrud(endpoints, "/themes");
    MapCrud(endpoints, "/api/themes");
    return endpoints;
  }

  private static void MapCrud(IEndpointRouteBuilder endpoints, string prefix)
  {
    var group = endpoints.MapGroup(prefix).RequireAuthorization();
    group.MapPost("/", CreateAsync);
    group.MapGet("/", ListAsync);
    group.MapGet("/{themeId:guid}", GetAsync);
    group.MapPatch("/{themeId:guid}", PatchAsync);
    group.MapDelete("/{themeId:guid}", DeleteAsync);
  }

  private static async Task<IResult> CreateAsync(
    HttpContext httpContext,
    CreateThemeRequestDto request,
    IThemeService themeService,
    ICurrentUserAccessor currentUserAccessor,
    CancellationToken cancellationToken)
  {
    var result = await themeService.CreateAsync(
      currentUserAccessor.GetRequiredUserId(),
      request,
      cancellationToken);

    return result.ErrorCode switch
    {
      null => TypedResults.Created($"/themes/{result.Value!.Id}", result.Value),
      ThemeErrorCodes.AlreadyExists => ApiProblem.Conflict(httpContext, "Theme name already exists."),
      _ => ApiProblem.Validation(
        httpContext,
        ApiProblem.RequestError("Validation failed."),
        "Validation failed.")
    };
  }

  private static async Task<IResult> ListAsync(
    string? cursor,
    int? limit,
    IThemeService themeService,
    ICurrentUserAccessor currentUserAccessor,
    CancellationToken cancellationToken)
  {
    var result = await themeService.ListAsync(
      currentUserAccessor.GetRequiredUserId(),
      new ThemeListQuery(cursor, limit),
      cancellationToken);

    return TypedResults.Ok(result.Value);
  }

  private static async Task<IResult> GetAsync(
    HttpContext httpContext,
    Guid themeId,
    IThemeService themeService,
    ICurrentUserAccessor currentUserAccessor,
    CancellationToken cancellationToken)
  {
    var result = await themeService.GetByIdAsync(
      currentUserAccessor.GetRequiredUserId(),
      themeId,
      cancellationToken);

    return result.ErrorCode switch
    {
      null => TypedResults.Ok(result.Value),
      ThemeErrorCodes.NotFound => ApiProblem.NotFound(httpContext, "Theme not found."),
      ThemeErrorCodes.Forbidden => ApiProblem.Forbidden(httpContext, "Access denied."),
      _ => ApiProblem.Validation(
        httpContext,
        ApiProblem.RequestError("Validation failed."),
        "Validation failed.")
    };
  }

  private static async Task<IResult> PatchAsync(
    HttpContext httpContext,
    Guid themeId,
    UpdateThemeRequestDto request,
    IThemeService themeService,
    ICurrentUserAccessor currentUserAccessor,
    CancellationToken cancellationToken)
  {
    var result = await themeService.UpdateAsync(
      currentUserAccessor.GetRequiredUserId(),
      themeId,
      request,
      cancellationToken);

    return result.ErrorCode switch
    {
      null => TypedResults.Ok(result.Value),
      ThemeErrorCodes.NotFound => ApiProblem.NotFound(httpContext, "Theme not found."),
      ThemeErrorCodes.Forbidden => ApiProblem.Forbidden(httpContext, "Access denied."),
      ThemeErrorCodes.AlreadyExists => ApiProblem.Conflict(httpContext, "Theme name already exists."),
      _ => ApiProblem.Validation(
        httpContext,
        ApiProblem.RequestError("Validation failed."),
        "Validation failed.")
    };
  }

  private static async Task<IResult> DeleteAsync(
    HttpContext httpContext,
    Guid themeId,
    IThemeService themeService,
    ICurrentUserAccessor currentUserAccessor,
    CancellationToken cancellationToken)
  {
    var result = await themeService.DeleteAsync(
      currentUserAccessor.GetRequiredUserId(),
      themeId,
      cancellationToken);

    return result.ErrorCode switch
    {
      null => TypedResults.NoContent(),
      ThemeErrorCodes.NotFound => ApiProblem.NotFound(httpContext, "Theme not found."),
      ThemeErrorCodes.Forbidden => ApiProblem.Forbidden(httpContext, "Access denied."),
      _ => ApiProblem.Validation(
        httpContext,
        ApiProblem.RequestError("Validation failed."),
        "Validation failed.")
    };
  }
}
