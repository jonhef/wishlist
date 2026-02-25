using System.Text.Json;

namespace Wishlist.Api.Features.Themes;

public static class ThemeTokenDefaults
{
  public const string DefaultThemeName = "DefaultDarkPinkNeon";
  private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

  public static ThemeTokensDto CreateDefault()
  {
    return new ThemeTokensDto(
      1,
      new ThemeColorsDto(
        "#0b0610",
        "#120a1a",
        "#1a1024",
        "#f6eaf5",
        "#cbb7c9",
        "#2a1a33",
        "#f4a6c8",
        "#ffb7d9",
        "#ff3fb4",
        "#bfa2ff",
        "#ff5a7a",
        "#4de1c1",
        "#ffb86b",
        "#ff5a7a"),
      new ThemeTypographyDto(
        "\"Unbounded\", \"Avenir Next\", \"Segoe UI\", system-ui, sans-serif",
        "\"Space Grotesk\", \"Avenir Next\", \"Segoe UI\", system-ui, sans-serif",
        "\"JetBrains Mono\", \"SFMono-Regular\", Menlo, Monaco, Consolas, \"Liberation Mono\", monospace",
        -0.02m,
        true),
      new ThemeRadiiDto(10, 16, 24),
      new ThemeEffectsDto(
        "0 0 10px rgba(255,63,180,0.25)",
        "0 0 18px rgba(255,63,180,0.35)",
        "0 0 28px rgba(255,63,180,0.45)",
        true,
        1m,
        0.03m));
  }

  public static ThemeTokensDto ParseAndNormalize(string? tokensJson)
  {
    if (!string.IsNullOrWhiteSpace(tokensJson))
    {
      try
      {
        var parsed = JsonSerializer.Deserialize<ThemeTokensPatchDto>(tokensJson, JsonOptions);
        return Normalize(parsed);
      }
      catch
      {
      }
    }

    return CreateDefault();
  }

  public static ThemeTokensDto Normalize(ThemeTokensPatchDto? patch)
  {
    var defaults = CreateDefault();

    var colors = patch?.Colors;
    var typography = patch?.Typography;
    var radii = patch?.Radii;
    var effects = patch?.Effects;

    return new ThemeTokensDto(
      1,
      new ThemeColorsDto(
        PickColor(colors?.Bg0, defaults.Colors.Bg0),
        PickColor(colors?.Bg1, defaults.Colors.Bg1),
        PickColor(colors?.Bg2, defaults.Colors.Bg2),
        PickColor(colors?.Text, defaults.Colors.Text),
        PickColor(colors?.MutedText, defaults.Colors.MutedText),
        PickColor(colors?.Border, defaults.Colors.Border),
        PickColor(colors?.Primary, defaults.Colors.Primary),
        PickColor(colors?.PrimaryHover, defaults.Colors.PrimaryHover),
        PickColor(colors?.AccentNeon, defaults.Colors.AccentNeon),
        PickColor(colors?.Secondary, defaults.Colors.Secondary),
        PickColor(colors?.Danger, defaults.Colors.Danger),
        PickColor(colors?.Success, defaults.Colors.Success),
        PickColor(colors?.Warn, defaults.Colors.Warn),
        PickColor(colors?.Error, defaults.Colors.Error)),
      new ThemeTypographyDto(
        PickTypography(typography?.FontDisplay, defaults.Typography.FontDisplay),
        PickTypography(typography?.FontBody, defaults.Typography.FontBody),
        PickTypography(typography?.FontMono, defaults.Typography.FontMono),
        Clamp(typography?.LetterSpacingDisplay ?? defaults.Typography.LetterSpacingDisplay, -0.2m, 0.2m),
        typography?.DisplayFontEnabled ?? defaults.Typography.DisplayFontEnabled),
      new ThemeRadiiDto(
        Clamp(radii?.Sm ?? defaults.Radii.Sm, 0, 48),
        Clamp(radii?.Md ?? defaults.Radii.Md, 0, 64),
        Clamp(radii?.Lg ?? defaults.Radii.Lg, 0, 80)),
      new ThemeEffectsDto(
        PickShadow(effects?.GlowSm, defaults.Effects.GlowSm),
        PickShadow(effects?.GlowMd, defaults.Effects.GlowMd),
        PickShadow(effects?.GlowLg, defaults.Effects.GlowLg),
        effects?.GlowEnabled ?? defaults.Effects.GlowEnabled,
        Clamp(effects?.GlowIntensity ?? defaults.Effects.GlowIntensity, 0, 2),
        Clamp(effects?.NoiseOpacity ?? defaults.Effects.NoiseOpacity, 0, 0.2m)));
  }

  public static bool ValidatePatchForSave(ThemeTokensPatchDto? patch)
  {
    if (patch is null)
    {
      return false;
    }

    if (patch.SchemaVersion is { } version && version != 1)
    {
      return false;
    }

    var colors = patch.Colors;
    if (colors is null)
    {
      return false;
    }

    if (!IsNonEmpty(colors.Bg0) || !IsNonEmpty(colors.Text))
    {
      return false;
    }

    if (!ValidateOptionalText(colors.Bg1)
      || !ValidateOptionalText(colors.Bg2)
      || !ValidateOptionalText(colors.MutedText)
      || !ValidateOptionalText(colors.Border)
      || !ValidateOptionalText(colors.Primary)
      || !ValidateOptionalText(colors.PrimaryHover)
      || !ValidateOptionalText(colors.AccentNeon)
      || !ValidateOptionalText(colors.Secondary)
      || !ValidateOptionalText(colors.Danger)
      || !ValidateOptionalText(colors.Success)
      || !ValidateOptionalText(colors.Warn)
      || !ValidateOptionalText(colors.Error))
    {
      return false;
    }

    if (patch.Radii is { } radii)
    {
      if (!ValidateOptionalRange(radii.Sm, 0, 48)
        || !ValidateOptionalRange(radii.Md, 0, 64)
        || !ValidateOptionalRange(radii.Lg, 0, 80))
      {
        return false;
      }
    }

    if (patch.Typography is { } typography)
    {
      if (!ValidateOptionalText(typography.FontDisplay)
        || !ValidateOptionalText(typography.FontBody)
        || !ValidateOptionalText(typography.FontMono)
        || !ValidateOptionalRange(typography.LetterSpacingDisplay, -0.2m, 0.2m))
      {
        return false;
      }
    }

    if (patch.Effects is { } effects)
    {
      if (!ValidateOptionalText(effects.GlowSm)
        || !ValidateOptionalText(effects.GlowMd)
        || !ValidateOptionalText(effects.GlowLg)
        || !ValidateOptionalRange(effects.GlowIntensity, 0, 2)
        || !ValidateOptionalRange(effects.NoiseOpacity, 0, 0.2m))
      {
        return false;
      }
    }

    return true;
  }

  private static string PickColor(string? candidate, string fallback)
  {
    return IsNonEmpty(candidate) ? candidate!.Trim() : fallback;
  }

  private static string PickTypography(string? candidate, string fallback)
  {
    return IsNonEmpty(candidate) ? candidate!.Trim() : fallback;
  }

  private static string PickShadow(string? candidate, string fallback)
  {
    return IsNonEmpty(candidate) ? candidate!.Trim() : fallback;
  }

  private static bool IsNonEmpty(string? value)
  {
    return !string.IsNullOrWhiteSpace(value) && value.Trim().Length <= 256;
  }

  private static bool ValidateOptionalText(string? value)
  {
    if (value is null)
    {
      return true;
    }

    return IsNonEmpty(value);
  }

  private static bool ValidateOptionalRange(decimal? value, decimal min, decimal max)
  {
    if (value is null)
    {
      return true;
    }

    return value.Value >= min && value.Value <= max;
  }

  private static decimal Clamp(decimal value, decimal min, decimal max)
  {
    return Math.Max(min, Math.Min(max, value));
  }
}
