import type { ThemeTokens, ThemeTokensPatch } from "./model";

export const DEFAULT_THEME_NAME = "DefaultDarkPinkNeon";

export const defaultThemeTokens: ThemeTokens = {
  schemaVersion: 1,
  colors: {
    bg0: "#0b0610",
    bg1: "#120a1a",
    bg2: "#1a1024",
    text: "#f6eaf5",
    mutedText: "#cbb7c9",
    border: "#2a1a33",
    primary: "#f4a6c8",
    primaryHover: "#ffb7d9",
    accentNeon: "#ff3fb4",
    secondary: "#bfa2ff",
    danger: "#ff5a7a",
    success: "#4de1c1",
    warn: "#ffb86b",
    error: "#ff5a7a"
  },
  radii: {
    sm: 10,
    md: 16,
    lg: 24
  },
  typography: {
    fontDisplay: "\"Unbounded\", \"Avenir Next\", \"Segoe UI\", system-ui, sans-serif",
    fontBody: "\"Space Grotesk\", \"Avenir Next\", \"Segoe UI\", system-ui, sans-serif",
    fontMono: "\"JetBrains Mono\", \"SFMono-Regular\", Menlo, Monaco, Consolas, \"Liberation Mono\", monospace",
    letterSpacingDisplay: -0.02,
    displayFontEnabled: true
  },
  effects: {
    glowSm: "0 0 10px rgba(255,63,180,0.25)",
    glowMd: "0 0 18px rgba(255,63,180,0.35)",
    glowLg: "0 0 28px rgba(255,63,180,0.45)",
    glowEnabled: true,
    glowIntensity: 1,
    noiseOpacity: 0.03
  }
};

function pickText(value: unknown, fallback: string): string {
  if (typeof value !== "string") {
    return fallback;
  }

  const trimmed = value.trim();
  return trimmed.length > 0 ? trimmed : fallback;
}

function pickNumber(value: unknown, fallback: number, min: number, max: number): number {
  if (typeof value !== "number" || Number.isNaN(value)) {
    return fallback;
  }

  return Math.max(min, Math.min(max, value));
}

function pickBoolean(value: unknown, fallback: boolean): boolean {
  return typeof value === "boolean" ? value : fallback;
}

export function resolveThemeTokens(input: ThemeTokensPatch | ThemeTokens | null | undefined): ThemeTokens {
  const defaults = defaultThemeTokens;
  const patch = input ?? {};

  return {
    schemaVersion: 1,
    colors: {
      bg0: pickText(patch.colors?.bg0, defaults.colors.bg0),
      bg1: pickText(patch.colors?.bg1, defaults.colors.bg1),
      bg2: pickText(patch.colors?.bg2, defaults.colors.bg2),
      text: pickText(patch.colors?.text, defaults.colors.text),
      mutedText: pickText(patch.colors?.mutedText, defaults.colors.mutedText),
      border: pickText(patch.colors?.border, defaults.colors.border),
      primary: pickText(patch.colors?.primary, defaults.colors.primary),
      primaryHover: pickText(patch.colors?.primaryHover, defaults.colors.primaryHover),
      accentNeon: pickText(patch.colors?.accentNeon, defaults.colors.accentNeon),
      secondary: pickText(patch.colors?.secondary, defaults.colors.secondary),
      danger: pickText(patch.colors?.danger, defaults.colors.danger),
      success: pickText(patch.colors?.success, defaults.colors.success),
      warn: pickText(patch.colors?.warn, defaults.colors.warn),
      error: pickText(patch.colors?.error, defaults.colors.error)
    },
    radii: {
      sm: pickNumber(patch.radii?.sm, defaults.radii.sm, 0, 48),
      md: pickNumber(patch.radii?.md, defaults.radii.md, 0, 64),
      lg: pickNumber(patch.radii?.lg, defaults.radii.lg, 0, 80)
    },
    typography: {
      fontDisplay: pickText(patch.typography?.fontDisplay, defaults.typography.fontDisplay),
      fontBody: pickText(patch.typography?.fontBody, defaults.typography.fontBody),
      fontMono: pickText(patch.typography?.fontMono, defaults.typography.fontMono),
      letterSpacingDisplay: pickNumber(
        patch.typography?.letterSpacingDisplay,
        defaults.typography.letterSpacingDisplay,
        -0.2,
        0.2
      ),
      displayFontEnabled: pickBoolean(patch.typography?.displayFontEnabled, defaults.typography.displayFontEnabled)
    },
    effects: {
      glowSm: pickText(patch.effects?.glowSm, defaults.effects.glowSm),
      glowMd: pickText(patch.effects?.glowMd, defaults.effects.glowMd),
      glowLg: pickText(patch.effects?.glowLg, defaults.effects.glowLg),
      glowEnabled: pickBoolean(patch.effects?.glowEnabled, defaults.effects.glowEnabled),
      glowIntensity: pickNumber(patch.effects?.glowIntensity, defaults.effects.glowIntensity, 0, 2),
      noiseOpacity: pickNumber(patch.effects?.noiseOpacity, defaults.effects.noiseOpacity, 0, 0.2)
    }
  };
}

export function validateThemeTokensForSave(tokens: ThemeTokensPatch | ThemeTokens): Record<string, string> {
  const errors: Record<string, string> = {};

  if (tokens.schemaVersion !== undefined && tokens.schemaVersion !== 1) {
    errors.schemaVersion = "schemaVersion must be 1";
  }

  if (!tokens.colors?.bg0?.trim()) {
    errors["colors.bg0"] = "bg0 is required";
  }

  if (!tokens.colors?.text?.trim()) {
    errors["colors.text"] = "text is required";
  }

  return errors;
}

export function getThemeDraft(base: ThemeTokens): ThemeTokens {
  return {
    schemaVersion: 1,
    colors: { ...base.colors },
    typography: { ...base.typography },
    radii: { ...base.radii },
    effects: { ...base.effects }
  };
}
