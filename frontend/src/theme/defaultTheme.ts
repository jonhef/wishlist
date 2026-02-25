import type { ThemeTokens } from "./model";

export const defaultThemeTokens: ThemeTokens = {
  colors: {
    bg: "#f6f4ef",
    text: "#1f2937",
    primary: "#d97706",
    secondary: "#f4f1e8",
    muted: "#6b7280",
    border: "#dbcfc0"
  },
  typography: {
    fontFamily: "\"Trebuchet MS\", \"Lucida Sans Unicode\", \"Lucida Grande\", sans-serif",
    fontSizeBase: 16
  },
  radii: {
    sm: 8,
    md: 14,
    lg: 22
  },
  spacing: {
    xs: 4,
    sm: 8,
    md: 16,
    lg: 24
  }
};
