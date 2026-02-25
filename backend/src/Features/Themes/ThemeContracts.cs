namespace Wishlist.Api.Features.Themes;

public sealed record ThemeColorsDto(
  string Bg0,
  string Bg1,
  string Bg2,
  string Text,
  string MutedText,
  string Border,
  string Primary,
  string PrimaryHover,
  string AccentNeon,
  string Secondary,
  string Danger,
  string Success,
  string Warn,
  string Error);

public sealed record ThemeTypographyDto(
  string FontDisplay,
  string FontBody,
  string FontMono,
  decimal LetterSpacingDisplay,
  bool DisplayFontEnabled);

public sealed record ThemeRadiiDto(
  decimal Sm,
  decimal Md,
  decimal Lg);

public sealed record ThemeEffectsDto(
  string GlowSm,
  string GlowMd,
  string GlowLg,
  bool GlowEnabled,
  decimal GlowIntensity,
  decimal NoiseOpacity);

public sealed record ThemeTokensDto(
  int SchemaVersion,
  ThemeColorsDto Colors,
  ThemeTypographyDto Typography,
  ThemeRadiiDto Radii,
  ThemeEffectsDto Effects);

public sealed record ThemeColorsPatchDto(
  string? Bg0,
  string? Bg1,
  string? Bg2,
  string? Text,
  string? MutedText,
  string? Border,
  string? Primary,
  string? PrimaryHover,
  string? AccentNeon,
  string? Secondary,
  string? Danger,
  string? Success,
  string? Warn,
  string? Error);

public sealed record ThemeTypographyPatchDto(
  string? FontDisplay,
  string? FontBody,
  string? FontMono,
  decimal? LetterSpacingDisplay,
  bool? DisplayFontEnabled);

public sealed record ThemeRadiiPatchDto(
  decimal? Sm,
  decimal? Md,
  decimal? Lg);

public sealed record ThemeEffectsPatchDto(
  string? GlowSm,
  string? GlowMd,
  string? GlowLg,
  bool? GlowEnabled,
  decimal? GlowIntensity,
  decimal? NoiseOpacity);

public sealed record ThemeTokensPatchDto(
  int? SchemaVersion,
  ThemeColorsPatchDto? Colors,
  ThemeTypographyPatchDto? Typography,
  ThemeRadiiPatchDto? Radii,
  ThemeEffectsPatchDto? Effects);

public sealed record CreateThemeRequestDto(string Name, ThemeTokensPatchDto Tokens);

public sealed record UpdateThemeRequestDto(string? Name, ThemeTokensPatchDto? Tokens);

public sealed record ThemeListQuery(string? Cursor, int? Limit);

public sealed record ThemeDto(
  Guid Id,
  string Name,
  ThemeTokensDto Tokens,
  DateTime CreatedAtUtc);

public sealed record DefaultThemeDto(string Name, ThemeTokensDto Tokens);

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
