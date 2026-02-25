export type ThemeColors = {
  bg0: string;
  bg1: string;
  bg2: string;
  text: string;
  mutedText: string;
  border: string;
  primary: string;
  primaryHover: string;
  accentNeon: string;
  secondary: string;
  danger: string;
  success: string;
  warn: string;
  error: string;
};

export type ThemeTypography = {
  fontDisplay: string;
  fontBody: string;
  fontMono: string;
  letterSpacingDisplay: number;
  displayFontEnabled: boolean;
};

export type ThemeRadii = {
  sm: number;
  md: number;
  lg: number;
};

export type ThemeEffects = {
  glowSm: string;
  glowMd: string;
  glowLg: string;
  glowEnabled: boolean;
  glowIntensity: number;
  noiseOpacity: number;
};

export type ThemeTokens = {
  schemaVersion: 1;
  colors: ThemeColors;
  typography: ThemeTypography;
  radii: ThemeRadii;
  effects: ThemeEffects;
};

export type ThemeTokensPatch = {
  schemaVersion?: number;
  colors?: Partial<ThemeColors>;
  typography?: Partial<ThemeTypography>;
  radii?: Partial<ThemeRadii>;
  effects?: Partial<ThemeEffects>;
};

export type Theme = {
  id: string;
  name: string;
  tokens: ThemeTokens;
  createdAtUtc: string;
};

export type ThemePreset = {
  name: string;
  tokens: ThemeTokens;
};
