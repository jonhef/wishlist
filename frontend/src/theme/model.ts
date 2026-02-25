export type ThemeColors = {
  bg: string;
  text: string;
  primary: string;
  secondary: string;
  muted: string;
  border: string;
};

export type ThemeTypography = {
  fontFamily: string;
  fontSizeBase: number;
};

export type ThemeRadii = {
  sm: number;
  md: number;
  lg: number;
};

export type ThemeSpacing = {
  xs: number;
  sm: number;
  md: number;
  lg: number;
};

export type ThemeTokens = {
  colors: ThemeColors;
  typography: ThemeTypography;
  radii: ThemeRadii;
  spacing: ThemeSpacing;
};

export type Theme = {
  id: string;
  name: string;
  tokens: ThemeTokens;
  createdAtUtc: string;
};
