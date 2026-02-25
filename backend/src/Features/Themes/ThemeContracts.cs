namespace Wishlist.Api.Features.Themes;

public sealed record ThemeColorsDto(
  string Bg,
  string Text,
  string Primary,
  string Secondary,
  string Muted,
  string Border);

public sealed record ThemeTypographyDto(
  string FontFamily,
  decimal FontSizeBase);

public sealed record ThemeRadiiDto(
  decimal Sm,
  decimal Md,
  decimal Lg);

public sealed record ThemeSpacingDto(
  decimal Xs,
  decimal Sm,
  decimal Md,
  decimal Lg);

public sealed record ThemeTokensDto(
  ThemeColorsDto Colors,
  ThemeTypographyDto Typography,
  ThemeRadiiDto Radii,
  ThemeSpacingDto Spacing);

public sealed record CreateThemeRequestDto(string Name, ThemeTokensDto Tokens);

public sealed record UpdateThemeRequestDto(string? Name, ThemeTokensDto? Tokens);

public sealed record ThemeListQuery(string? Cursor, int? Limit);

public sealed record ThemeDto(
  Guid Id,
  string Name,
  ThemeTokensDto Tokens,
  DateTime CreatedAtUtc);

public sealed record ThemeListResult(IReadOnlyList<ThemeDto> Items, string? NextCursor);

public sealed record ThemeServiceResult<T>(T? Value, string? ErrorCode)
{
  public bool IsSuccess => ErrorCode is null;

  public static ThemeServiceResult<T> Success(T value) => new(value, null);

  public static ThemeServiceResult<T> Failure(string errorCode) => new(default, errorCode);
}

public static class ThemeErrorCodes
{
  public const string ValidationFailed = "validation_failed";
  public const string NotFound = "not_found";
  public const string Forbidden = "forbidden";
  public const string AlreadyExists = "already_exists";
}
