import type { CSSProperties } from "react";
import type { ThemeTokens } from "./model";

const px = (value: number): string => `${value}px`;

export function themeTokensToCssVars(tokens: ThemeTokens): Record<string, string> {
  return {
    "--color-bg": tokens.colors.bg,
    "--color-text": tokens.colors.text,
    "--color-primary": tokens.colors.primary,
    "--color-secondary": tokens.colors.secondary,
    "--color-muted": tokens.colors.muted,
    "--color-border": tokens.colors.border,
    "--font-family-base": tokens.typography.fontFamily,
    "--font-size-base": px(tokens.typography.fontSizeBase),
    "--radius-sm": px(tokens.radii.sm),
    "--radius-md": px(tokens.radii.md),
    "--radius-lg": px(tokens.radii.lg),
    "--space-xs": px(tokens.spacing.xs),
    "--space-sm": px(tokens.spacing.sm),
    "--space-md": px(tokens.spacing.md),
    "--space-lg": px(tokens.spacing.lg)
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
