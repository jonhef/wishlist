import { FormEvent, useEffect, useMemo, useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { apiClient } from "../../api/client";
import { defaultThemeTokens, getThemeDraft, validateThemeTokensForSave } from "../../theme/defaultTokens";
import { bodyFontOptions, displayFontOptions, monoFontOptions } from "../../theme/fonts";
import { useTheme } from "../../theme/ThemeProvider";
import type { ThemeTokens } from "../../theme/model";
import { Button, Card, Input, useToast } from "../../ui";

type PreviewTab = "overview" | "items" | "sharing";

type ThemeColorKey = keyof ThemeTokens["colors"];

const editableColorKeys: ThemeColorKey[] = [
  "bg0",
  "bg1",
  "bg2",
  "text",
  "mutedText",
  "border",
  "primary",
  "accentNeon",
  "secondary"
];

function toTitleCase(input: string): string {
  return input.slice(0, 1).toUpperCase() + input.slice(1);
}

export function ThemeEditorPage(): JSX.Element {
  const queryClient = useQueryClient();
  const { showToast } = useToast();
  const { themes, activeThemeId, appliedTokens, setPreviewTokens, setActiveTheme, upsertTheme, refreshThemes } = useTheme();

  const [selectedThemeId, setSelectedThemeId] = useState<string>(activeThemeId ?? "");
  const [themeName, setThemeName] = useState("New Theme");
  const [draft, setDraft] = useState<ThemeTokens>(getThemeDraft(appliedTokens));
  const [wishlistId, setWishlistId] = useState("");
  const [fieldErrors, setFieldErrors] = useState<Record<string, string>>({});
  const [previewTab, setPreviewTab] = useState<PreviewTab>("overview");

  const wishlistQuery = useQuery({
    queryKey: ["wishlists", "for-theme-editor"],
    queryFn: () => apiClient.listWishlists(undefined, 100)
  });

  useEffect(() => {
    if (!selectedThemeId) {
      return;
    }

    const selectedTheme = themes.find((theme) => theme.id === selectedThemeId);

    if (!selectedTheme) {
      return;
    }

    setThemeName(selectedTheme.name);
    setDraft(getThemeDraft(selectedTheme.tokens));
  }, [selectedThemeId, themes]);

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
      setFieldErrors({});
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
    fontFamily: draft.typography.fontBody
  }), [draft]);

  const onSubmit = (event: FormEvent<HTMLFormElement>): void => {
    event.preventDefault();

    if (!themeName.trim()) {
      showToast("Theme name required", "error");
      return;
    }

    const tokenErrors = validateThemeTokensForSave(draft);
    setFieldErrors(tokenErrors);

    if (Object.keys(tokenErrors).length > 0) {
      showToast("Fill required token fields", "error");
      return;
    }

    saveMutation.mutate();
  };

  return (
    <section className="stack gap-lg">
      <header className="section-header">
        <div>
          <h2>Theme editor</h2>
          <p className="muted">Tune dark pastel + subtle neon through tokens only.</p>
        </div>
        <Button
          onClick={() => {
            setSelectedThemeId("");
            setThemeName("New Theme");
            setDraft(getThemeDraft(defaultThemeTokens));
            setFieldErrors({});
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
                className="ui-input glow-focus"
                id="theme-source"
                onChange={(event) => {
                  const nextThemeId = event.target.value;
                  setSelectedThemeId(nextThemeId);

                  if (!nextThemeId) {
                    setThemeName("New Theme");
                    setDraft(getThemeDraft(defaultThemeTokens));
                    setFieldErrors({});
                  }
                }}
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

            <div className="stack">
              <h3>Colors</h3>
              <div className="grid-three">
                {editableColorKeys.map((key) => (
                  <label className="ui-field" htmlFor={`theme-color-${key}`} key={key}>
                    <span className="ui-field-label">{toTitleCase(key)}</span>
                    <input
                      className="ui-input color-input glow-focus"
                      id={`theme-color-${key}`}
                      onChange={(event) => setDraft({
                        ...draft,
                        colors: {
                          ...draft.colors,
                          [key]: event.target.value
                        }
                      })}
                      type="color"
                      value={draft.colors[key]}
                    />
                    {fieldErrors[`colors.${key}`] ? <span className="form-error">{fieldErrors[`colors.${key}`]}</span> : null}
                  </label>
                ))}
              </div>
            </div>

            <div className="stack">
              <h3>Typography</h3>
              <div className="grid-two">
                <label className="ui-field" htmlFor="theme-font-display">
                  <span className="ui-field-label">Display font</span>
                  <select
                    className="ui-input glow-focus"
                    id="theme-font-display"
                    onChange={(event) => setDraft({
                      ...draft,
                      typography: {
                        ...draft.typography,
                        fontDisplay: event.target.value
                      }
                    })}
                    value={draft.typography.fontDisplay}
                  >
                    {displayFontOptions.map((option) => (
                      <option key={option.value} value={option.value}>
                        {option.label}
                      </option>
                    ))}
                  </select>
                </label>

                <label className="ui-field" htmlFor="theme-font-body">
                  <span className="ui-field-label">Body font</span>
                  <select
                    className="ui-input glow-focus"
                    id="theme-font-body"
                    onChange={(event) => setDraft({
                      ...draft,
                      typography: {
                        ...draft.typography,
                        fontBody: event.target.value
                      }
                    })}
                    value={draft.typography.fontBody}
                  >
                    {bodyFontOptions.map((option) => (
                      <option key={option.value} value={option.value}>
                        {option.label}
                      </option>
                    ))}
                  </select>
                </label>
              </div>

              <div className="grid-two">
                <label className="ui-field" htmlFor="theme-font-mono">
                  <span className="ui-field-label">Mono font</span>
                  <select
                    className="ui-input glow-focus"
                    id="theme-font-mono"
                    onChange={(event) => setDraft({
                      ...draft,
                      typography: {
                        ...draft.typography,
                        fontMono: event.target.value
                      }
                    })}
                    value={draft.typography.fontMono}
                  >
                    {monoFontOptions.map((option) => (
                      <option key={option.value} value={option.value}>
                        {option.label}
                      </option>
                    ))}
                  </select>
                </label>

                <label className="ui-field" htmlFor="theme-letter-spacing">
                  <span className="ui-field-label">
                    Display letter spacing ({draft.typography.letterSpacingDisplay.toFixed(2)}em)
                  </span>
                  <input
                    className="ui-input"
                    id="theme-letter-spacing"
                    max={0.08}
                    min={-0.12}
                    onChange={(event) => {
                      const value = Number(event.target.value);
                      setDraft({
                        ...draft,
                        typography: {
                          ...draft.typography,
                          letterSpacingDisplay: value
                        }
                      });
                    }}
                    step={0.01}
                    type="range"
                    value={draft.typography.letterSpacingDisplay}
                  />
                </label>
              </div>

              <label className="switch-row" htmlFor="theme-display-font-enabled">
                <input
                  checked={draft.typography.displayFontEnabled}
                  id="theme-display-font-enabled"
                  onChange={(event) => setDraft({
                    ...draft,
                    typography: {
                      ...draft.typography,
                      displayFontEnabled: event.target.checked
                    }
                  })}
                  type="checkbox"
                />
                <span>Use display font on headings</span>
              </label>
            </div>

            <div className="stack">
              <h3>Effects</h3>

              <label className="switch-row" htmlFor="theme-glow-enabled">
                <input
                  checked={draft.effects.glowEnabled}
                  id="theme-glow-enabled"
                  onChange={(event) => setDraft({
                    ...draft,
                    effects: {
                      ...draft.effects,
                      glowEnabled: event.target.checked
                    }
                  })}
                  type="checkbox"
                />
                <span>Glow enabled</span>
              </label>

              <label className="ui-field" htmlFor="theme-glow-intensity">
                <span className="ui-field-label">Glow intensity multiplier ({draft.effects.glowIntensity.toFixed(2)}x)</span>
                <input
                  className="ui-input"
                  id="theme-glow-intensity"
                  max={2}
                  min={0}
                  onChange={(event) => {
                    const value = Number(event.target.value);
                    setDraft({
                      ...draft,
                      effects: {
                        ...draft.effects,
                        glowIntensity: value
                      }
                    });
                  }}
                  step={0.05}
                  type="range"
                  value={draft.effects.glowIntensity}
                />
              </label>

              <label className="ui-field" htmlFor="theme-noise-opacity">
                <span className="ui-field-label">Noise opacity ({draft.effects.noiseOpacity.toFixed(3)})</span>
                <input
                  className="ui-input"
                  id="theme-noise-opacity"
                  max={0.2}
                  min={0}
                  onChange={(event) => {
                    const value = Number(event.target.value);
                    setDraft({
                      ...draft,
                      effects: {
                        ...draft.effects,
                        noiseOpacity: value
                      }
                    });
                  }}
                  step={0.005}
                  type="range"
                  value={draft.effects.noiseOpacity}
                />
              </label>
            </div>

            <div className="actions-row">
              <Button disabled={saveMutation.isPending} type="submit">
                Save theme
              </Button>
            </div>
          </form>
        </Card>

        <Card className="stack gap-md">
          <h3>Preview</h3>
          <div className="preview-panel stack gap-md" style={previewStyle}>
            <div className="ui-tab-list" role="tablist" aria-label="Preview tabs">
              <button
                aria-selected={previewTab === "overview"}
                className="ui-tab glow-focus"
                onClick={() => setPreviewTab("overview")}
                role="tab"
                type="button"
              >
                Overview
              </button>
              <button
                aria-selected={previewTab === "items"}
                className="ui-tab glow-focus"
                onClick={() => setPreviewTab("items")}
                role="tab"
                type="button"
              >
                Items
              </button>
              <button
                aria-selected={previewTab === "sharing"}
                className="ui-tab glow-focus"
                onClick={() => setPreviewTab("sharing")}
                role="tab"
                type="button"
              >
                Sharing
              </button>
            </div>

            <Card className="stack gap-md">
              <h3>Interactive sample</h3>
              <Input id="preview-input" label="Input focus" placeholder="Tab here" />
              <div className="actions-row">
                <Button>Primary</Button>
                <Button variant="secondary">Secondary</Button>
                <Button variant="ghost">Ghost</Button>
              </div>
              <p className="muted">Hover card/button, focus input, switch tabs to check glow behavior.</p>
            </Card>

            <div className="stack">
              <h3>Token checks</h3>
              <div className="token-row">
                <span className="muted">Glow sm</span>
                <span className="token-chip">{draft.effects.glowSm}</span>
              </div>
              <div className="token-row">
                <span className="muted">Glow md</span>
                <span className="token-chip">{draft.effects.glowMd}</span>
              </div>
              <div className="token-row">
                <span className="muted">Glow lg</span>
                <span className="token-chip">{draft.effects.glowLg}</span>
              </div>
            </div>
          </div>
        </Card>
      </div>

      <Card className="stack gap-md">
        <h3>Apply theme to wishlist</h3>

        <label className="ui-field" htmlFor="apply-wishlist">
          <span className="ui-field-label">Wishlist</span>
          <select
            className="ui-input glow-focus"
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
