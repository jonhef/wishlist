import { createContext, useCallback, useContext, useEffect, useMemo, useState } from "react";
import type { ReactNode } from "react";
import { apiClient } from "../api/client";
import { useAuth } from "../auth/AuthProvider";
import { applyThemeTokens } from "./cssVars";
import { defaultThemeTokens } from "./defaultTheme";
import type { Theme, ThemeTokens } from "./model";

type ThemeContextValue = {
  themes: Theme[];
  isLoadingThemes: boolean;
  activeThemeId: string | null;
  appliedTokens: ThemeTokens;
  setActiveTheme: (themeId: string | null) => void;
  setPreviewTokens: (tokens: ThemeTokens | null) => void;
  refreshThemes: () => Promise<void>;
  upsertTheme: (theme: Theme) => void;
};

const ThemeContext = createContext<ThemeContextValue | undefined>(undefined);

function storageKey(email: string | null): string {
  return `wishlist.theme.active:${email ?? "anon"}`;
}

function pickInitialTheme(themes: Theme[], savedThemeId: string | null): Theme | null {
  if (!themes.length) {
    return null;
  }

  if (savedThemeId) {
    const saved = themes.find((theme) => theme.id === savedThemeId);

    if (saved) {
      return saved;
    }
  }

  return themes[0] ?? null;
}

export function ThemeProvider({ children }: { children: ReactNode }): JSX.Element {
  const { email, isAuthenticated } = useAuth();
  const [themes, setThemes] = useState<Theme[]>([]);
  const [isLoadingThemes, setIsLoadingThemes] = useState(false);
  const [activeThemeId, setActiveThemeId] = useState<string | null>(null);
  const [activeTokens, setActiveTokens] = useState<ThemeTokens>(defaultThemeTokens);
  const [previewTokens, setPreviewTokensState] = useState<ThemeTokens | null>(null);

  const refreshThemes = useCallback(async (): Promise<void> => {
    if (!isAuthenticated) {
      setThemes([]);
      setActiveThemeId(null);
      setActiveTokens(defaultThemeTokens);
      return;
    }

    setIsLoadingThemes(true);

    try {
      const payload = await apiClient.listThemes(undefined, 100);
      const fetchedThemes = payload.items;
      const savedThemeId = localStorage.getItem(storageKey(email));
      const selected = pickInitialTheme(fetchedThemes, savedThemeId);

      setThemes(fetchedThemes);
      setActiveThemeId(selected?.id ?? null);
      setActiveTokens(selected?.tokens ?? defaultThemeTokens);
    } catch {
      setThemes([]);
      setActiveThemeId(null);
      setActiveTokens(defaultThemeTokens);
    } finally {
      setIsLoadingThemes(false);
    }
  }, [email, isAuthenticated]);

  useEffect(() => {
    void refreshThemes();
  }, [refreshThemes]);

  useEffect(() => {
    if (activeThemeId) {
      localStorage.setItem(storageKey(email), activeThemeId);
    } else {
      localStorage.removeItem(storageKey(email));
    }
  }, [activeThemeId, email]);

  useEffect(() => {
    const targetTokens = previewTokens ?? activeTokens;
    applyThemeTokens(targetTokens);
  }, [activeTokens, previewTokens]);

  const value = useMemo<ThemeContextValue>(() => ({
    themes,
    isLoadingThemes,
    activeThemeId,
    appliedTokens: previewTokens ?? activeTokens,
    setActiveTheme: (themeId: string | null) => {
      setActiveThemeId(themeId);

      if (!themeId) {
        setActiveTokens(defaultThemeTokens);
        return;
      }

      const match = themes.find((theme) => theme.id === themeId);
      setActiveTokens(match?.tokens ?? defaultThemeTokens);
    },
    setPreviewTokens: (tokens: ThemeTokens | null) => {
      setPreviewTokensState(tokens);
    },
    refreshThemes,
    upsertTheme: (theme: Theme) => {
      setThemes((currentThemes) => {
        const existing = currentThemes.find((entry) => entry.id === theme.id);

        if (!existing) {
          return [theme, ...currentThemes];
        }

        return currentThemes.map((entry) => (entry.id === theme.id ? theme : entry));
      });
      setActiveThemeId(theme.id);
      setActiveTokens(theme.tokens);
    }
  }), [activeThemeId, activeTokens, isLoadingThemes, previewTokens, refreshThemes, themes]);

  return <ThemeContext.Provider value={value}>{children}</ThemeContext.Provider>;
}

export function useTheme(): ThemeContextValue {
  const context = useContext(ThemeContext);

  if (!context) {
    throw new Error("useTheme must be used inside ThemeProvider");
  }

  return context;
}
