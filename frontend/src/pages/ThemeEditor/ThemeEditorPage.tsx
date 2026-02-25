import { FormEvent, useEffect, useMemo, useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { apiClient } from "../../api/client";
import { defaultThemeTokens } from "../../theme/defaultTheme";
import { useTheme } from "../../theme/ThemeProvider";
import type { ThemeTokens } from "../../theme/model";
import { Button, Card, Input, useToast } from "../../ui";

const fontOptions = [
  { label: "Trebuchet", value: "\"Trebuchet MS\", \"Lucida Sans Unicode\", \"Lucida Grande\", sans-serif" },
  { label: "Georgia", value: "Georgia, Cambria, \"Times New Roman\", serif" },
  { label: "Fira Sans", value: "\"Fira Sans\", \"Avenir Next\", \"Segoe UI\", sans-serif" },
  { label: "Courier Prime", value: "\"Courier Prime\", \"Courier New\", monospace" }
] as const;

function getThemeDraft(base: ThemeTokens): ThemeTokens {
  return {
    colors: {
      ...base.colors
    },
    typography: {
      ...base.typography
    },
    radii: {
      ...base.radii
    },
    spacing: {
      ...base.spacing
    }
  };
}

export function ThemeEditorPage(): JSX.Element {
  const queryClient = useQueryClient();
  const { showToast } = useToast();
  const { themes, activeThemeId, appliedTokens, setPreviewTokens, setActiveTheme, upsertTheme, refreshThemes } = useTheme();

  const [selectedThemeId, setSelectedThemeId] = useState<string>(activeThemeId ?? "");
  const [themeName, setThemeName] = useState("New Theme");
  const [draft, setDraft] = useState<ThemeTokens>(getThemeDraft(appliedTokens));
  const [wishlistId, setWishlistId] = useState("");

  const wishlistQuery = useQuery({
    queryKey: ["wishlists", "for-theme-editor"],
    queryFn: () => apiClient.listWishlists(undefined, 100)
  });

  useEffect(() => {
    const selectedTheme = themes.find((theme) => theme.id === selectedThemeId);

    if (selectedTheme) {
      setThemeName(selectedTheme.name);
      setDraft(getThemeDraft(selectedTheme.tokens));
      return;
    }

    setDraft(getThemeDraft(appliedTokens));
  }, [selectedThemeId, themes, appliedTokens]);

  useEffect(() => {
    setPreviewTokens(draft);
  }, [draft, setPreviewTokens]);

  useEffect(() => () => {
    setPreviewTokens(null);
  }, [setPreviewTokens]);

  useEffect(() => {
    if (!activeThemeId && themes.length) {
      setSelectedThemeId(themes[0].id);
      return;
    }

    if (activeThemeId) {
      setSelectedThemeId(activeThemeId);
    }
  }, [activeThemeId, themes]);

  const saveMutation = useMutation({
    mutationFn: async () => {
      if (selectedThemeId) {
        return apiClient.patchTheme(selectedThemeId, {
          name: themeName.trim(),
          tokens: draft
        });
      }

      return apiClient.createTheme({
        name: themeName.trim(),
        tokens: draft
      });
    },
    onSuccess: async (theme) => {
      upsertTheme(theme);
      setSelectedThemeId(theme.id);
      setActiveTheme(theme.id);
      setPreviewTokens(null);
      await refreshThemes();
      showToast("Theme saved", "success");
    },
    onError: () => {
      showToast("Could not save theme", "error");
    }
  });

  const applyMutation = useMutation({
    mutationFn: async () => {
      if (!wishlistId) {
        throw new Error("Pick wishlist");
      }

      if (!selectedThemeId) {
        throw new Error("Save theme first");
      }

      return apiClient.patchWishlist(wishlistId, {
        themeId: selectedThemeId
      });
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["wishlists"] });
      showToast("Theme applied to wishlist", "success");
    },
    onError: () => {
      showToast("Could not apply theme", "error");
    }
  });

  const previewStyle = useMemo(() => ({
    fontFamily: draft.typography.fontFamily,
    fontSize: `${draft.typography.fontSizeBase}px`
  }), [draft]);

  const onSubmit = (event: FormEvent<HTMLFormElement>): void => {
    event.preventDefault();

    if (!themeName.trim()) {
      showToast("Theme name required", "error");
      return;
    }

    saveMutation.mutate();
  };

  const mdRadius = draft.radii.md;

  return (
    <section className="stack gap-lg">
      <header className="section-header">
        <div>
          <h2>Theme editor</h2>
          <p className="muted">Preview updates instantly, persistence only after save.</p>
        </div>
        <Button
          onClick={() => {
            setSelectedThemeId("");
            setThemeName("New Theme");
            setDraft(getThemeDraft(defaultThemeTokens));
          }}
          variant="secondary"
        >
          New theme
        </Button>
      </header>

      <div className="editor-layout">
        <Card>
          <form className="stack gap-md" onSubmit={onSubmit}>
            <label className="ui-field" htmlFor="theme-source">
              <span className="ui-field-label">Source theme</span>
              <select
                className="ui-input"
                id="theme-source"
                onChange={(event) => setSelectedThemeId(event.target.value)}
                value={selectedThemeId}
              >
                <option value="">Unsaved new theme</option>
                {themes.map((theme) => (
                  <option key={theme.id} value={theme.id}>
                    {theme.name}
                  </option>
                ))}
              </select>
            </label>

            <Input
              id="theme-name"
              label="Theme name"
              onChange={(event) => setThemeName(event.target.value)}
              required
              value={themeName}
            />

            <div className="grid-three">
              <label className="ui-field" htmlFor="theme-bg">
                <span className="ui-field-label">Background</span>
                <input
                  className="ui-input color-input"
                  id="theme-bg"
                  onChange={(event) => setDraft({
                    ...draft,
                    colors: {
                      ...draft.colors,
                      bg: event.target.value
                    }
                  })}
                  type="color"
                  value={draft.colors.bg}
                />
              </label>

              <label className="ui-field" htmlFor="theme-primary">
                <span className="ui-field-label">Primary</span>
                <input
                  className="ui-input color-input"
                  id="theme-primary"
                  onChange={(event) => setDraft({
                    ...draft,
                    colors: {
                      ...draft.colors,
                      primary: event.target.value
                    }
                  })}
                  type="color"
                  value={draft.colors.primary}
                />
              </label>

              <label className="ui-field" htmlFor="theme-text">
                <span className="ui-field-label">Text</span>
                <input
                  className="ui-input color-input"
                  id="theme-text"
                  onChange={(event) => setDraft({
                    ...draft,
                    colors: {
                      ...draft.colors,
                      text: event.target.value
                    }
                  })}
                  type="color"
                  value={draft.colors.text}
                />
              </label>
            </div>

            <label className="ui-field" htmlFor="theme-radius">
              <span className="ui-field-label">Radius ({mdRadius}px)</span>
              <input
                className="ui-input"
                id="theme-radius"
                max={28}
                min={4}
                onChange={(event) => {
                  const radius = Number(event.target.value);
                  setDraft({
                    ...draft,
                    radii: {
                      sm: Math.max(2, radius - 4),
                      md: radius,
                      lg: radius + 8
                    }
                  });
                }}
                type="range"
                value={mdRadius}
              />
            </label>

            <label className="ui-field" htmlFor="theme-font">
              <span className="ui-field-label">Font</span>
              <select
                className="ui-input"
                id="theme-font"
                onChange={(event) => setDraft({
                  ...draft,
                  typography: {
                    ...draft.typography,
                    fontFamily: event.target.value
                  }
                })}
                value={draft.typography.fontFamily}
              >
                {fontOptions.map((option) => (
                  <option key={option.value} value={option.value}>
                    {option.label}
                  </option>
                ))}
              </select>
            </label>

            <div className="actions-row">
              <Button disabled={saveMutation.isPending} type="submit">
                Save theme
              </Button>
            </div>
          </form>
        </Card>

        <Card className="stack gap-md">
          <h3>Preview</h3>
          <div className="preview-panel" style={previewStyle}>
            <Card>
              <p>Card sample using current tokens.</p>
              <Input id="preview-input" label="Input" placeholder="Try typing" />
              <div className="actions-row">
                <Button>Primary</Button>
                <Button variant="secondary">Secondary</Button>
              </div>
            </Card>
          </div>
        </Card>
      </div>

      <Card className="stack gap-md">
        <h3>Apply theme to wishlist</h3>

        <label className="ui-field" htmlFor="apply-wishlist">
          <span className="ui-field-label">Wishlist</span>
          <select
            className="ui-input"
            id="apply-wishlist"
            onChange={(event) => setWishlistId(event.target.value)}
            value={wishlistId}
          >
            <option value="">Select wishlist</option>
            {(wishlistQuery.data?.items ?? []).map((wishlist) => (
              <option key={wishlist.id} value={wishlist.id}>
                {wishlist.title}
              </option>
            ))}
          </select>
        </label>

        <Button disabled={!wishlistId || !selectedThemeId || applyMutation.isPending} onClick={() => applyMutation.mutate()}>
          Apply selected theme
        </Button>
      </Card>
    </section>
  );
}
