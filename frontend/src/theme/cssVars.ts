import type { CSSProperties } from "react";
import type { ThemeTokens } from "./model";

const px = (value: number): string => `${value}px`;

function scaleGlowShadow(shadow: string, intensity: number): string {
  return shadow.replace(/rgba\((\d+),\s*(\d+),\s*(\d+),\s*([\d.]+)\)/g, (_, red, green, blue, alpha) => {
    const scaled = Math.max(0, Math.min(1, Number(alpha) * intensity));
    return `rgba(${red},${green},${blue},${scaled.toFixed(3)})`;
  });
}

function resolveGlow(shadow: string, enabled: boolean, intensity: number): string {
  if (!enabled) {
    return "none";
  }

  return scaleGlowShadow(shadow, intensity);
}

export function themeTokensToCssVars(tokens: ThemeTokens): Record<string, string> {
  const resolvedDisplayFont = tokens.typography.displayFontEnabled
    ? tokens.typography.fontDisplay
    : tokens.typography.fontBody;

  return {
    "--color-bg0": tokens.colors.bg0,
    "--color-bg1": tokens.colors.bg1,
    "--color-bg2": tokens.colors.bg2,
    "--color-text": tokens.colors.text,
    "--color-muted-text": tokens.colors.mutedText,
    "--color-border": tokens.colors.border,
    "--color-primary": tokens.colors.primary,
    "--color-primary-hover": tokens.colors.primaryHover,
    "--color-accent-neon": tokens.colors.accentNeon,
    "--color-secondary": tokens.colors.secondary,
    "--color-danger": tokens.colors.danger,
    "--color-success": tokens.colors.success,
    "--color-warn": tokens.colors.warn,
    "--color-error": tokens.colors.error,
    "--font-family-display": resolvedDisplayFont,
    "--font-family-body": tokens.typography.fontBody,
    "--font-family-mono": tokens.typography.fontMono,
    "--letter-spacing-display": `${tokens.typography.letterSpacingDisplay}em`,
    "--radius-sm": px(tokens.radii.sm),
    "--radius-md": px(tokens.radii.md),
    "--radius-lg": px(tokens.radii.lg),
    "--effect-glow-sm": resolveGlow(tokens.effects.glowSm, tokens.effects.glowEnabled, tokens.effects.glowIntensity),
    "--effect-glow-md": resolveGlow(tokens.effects.glowMd, tokens.effects.glowEnabled, tokens.effects.glowIntensity),
    "--effect-glow-lg": resolveGlow(tokens.effects.glowLg, tokens.effects.glowEnabled, tokens.effects.glowIntensity),
    "--effect-noise-opacity": String(tokens.effects.noiseOpacity)
  };
}

export function themeTokensToStyle(tokens: ThemeTokens): CSSProperties {
  return themeTokensToCssVars(tokens) as CSSProperties;
}

export function applyThemeTokens(tokens: ThemeTokens, target: HTMLElement = document.documentElement): void {
  const vars = themeTokensToCssVars(tokens);

  Object.entries(vars).forEach(([key, value]) => {
    target.style.setProperty(key, value);
  });
}
