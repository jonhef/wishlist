import { z } from "zod";
import { resolveThemeTokens } from "../theme/defaultTokens";
import type { Theme, ThemePreset, ThemeTokensPatch } from "../theme/model";

const zIsoDate = z.string().min(1);

const themeTokensPatchSchema = z.object({
  schemaVersion: z.number().int().optional(),
  colors: z.object({
    bg0: z.string().optional(),
    bg1: z.string().optional(),
    bg2: z.string().optional(),
    text: z.string().optional(),
    mutedText: z.string().optional(),
    border: z.string().optional(),
    primary: z.string().optional(),
    primaryHover: z.string().optional(),
    accentNeon: z.string().optional(),
    secondary: z.string().optional(),
    danger: z.string().optional(),
    success: z.string().optional(),
    warn: z.string().optional(),
    error: z.string().optional()
  }).optional(),
  typography: z.object({
    fontDisplay: z.string().optional(),
    fontBody: z.string().optional(),
    fontMono: z.string().optional(),
    letterSpacingDisplay: z.number().optional(),
    displayFontEnabled: z.boolean().optional()
  }).optional(),
  radii: z.object({
    sm: z.number().optional(),
    md: z.number().optional(),
    lg: z.number().optional()
  }).optional(),
  effects: z.object({
    glowSm: z.string().optional(),
    glowMd: z.string().optional(),
    glowLg: z.string().optional(),
    glowEnabled: z.boolean().optional(),
    glowIntensity: z.number().optional(),
    noiseOpacity: z.number().optional()
  }).optional()
});

const resolvedThemeTokensSchema = themeTokensPatchSchema.transform((tokens) => resolveThemeTokens(tokens));

const authTokensSchema = z.object({
  accessToken: z.string(),
  accessTokenExpiresAtUtc: zIsoDate,
  refreshToken: z.string().nullable().optional(),
  refreshTokenExpiresAtUtc: zIsoDate
});

const registerResponseSchema = z.object({
  userId: z.string().uuid(),
  email: z.string().email()
});

const wishlistSchema = z.object({
  id: z.string().uuid(),
  title: z.string(),
  description: z.string().nullable(),
  themeId: z.string().uuid().nullable(),
  updatedAt: zIsoDate,
  itemsCount: z.number()
});

const wishlistListSchema = z.object({
  items: z.array(wishlistSchema),
  nextCursor: z.string().nullable()
});

const itemSchema = z.object({
  id: z.number(),
  wishlistId: z.string().uuid(),
  name: z.string(),
  url: z.string().nullable(),
  priceAmount: z.number().nullable(),
  priceCurrency: z.string().nullable(),
  priority: z.number(),
  notes: z.string().nullable(),
  updatedAtUtc: zIsoDate
});

const itemListSchema = z.object({
  items: z.array(itemSchema),
  nextCursor: z.string().nullable()
});

const themeSchema = z.object({
  id: z.string().uuid(),
  name: z.string(),
  tokens: resolvedThemeTokensSchema,
  createdAtUtc: zIsoDate
});

const themeListSchema = z.object({
  items: z.array(themeSchema),
  nextCursor: z.string().nullable()
});

const publicWishlistSchema = z.object({
  title: z.string(),
  description: z.string().nullable(),
  themeTokens: resolvedThemeTokensSchema,
  items: z.array(
    z.object({
      name: z.string(),
      url: z.string().nullable(),
      priceAmount: z.number().nullable(),
      priceCurrency: z.string().nullable(),
      priority: z.number(),
      notes: z.string().nullable()
    })
  ),
  nextCursor: z.string().nullable()
});

const defaultThemeSchema = z.object({
  name: z.string(),
  tokens: resolvedThemeTokensSchema
});

const shareRotationSchema = z.object({
  publicUrl: z.string().url()
});

export type AuthTokensResponse = z.infer<typeof authTokensSchema>;
export type StoredAuthTokens = {
  accessToken: string;
  accessTokenExpiresAtUtc: string;
  refreshToken: string | null;
  refreshTokenExpiresAtUtc: string;
};

export type RegisterRequest = {
  email: string;
  password: string;
};

export type LoginRequest = {
  email: string;
  password: string;
};

export type Wishlist = z.infer<typeof wishlistSchema>;
export type WishlistListResult = z.infer<typeof wishlistListSchema>;

export type CreateWishlistRequest = {
  title: string;
  description?: string | null;
  themeId?: string | null;
};

export type UpdateWishlistRequest = {
  title?: string | null;
  description?: string | null;
  themeId?: string | null;
};

export type Item = z.infer<typeof itemSchema>;
export type ItemListResult = z.infer<typeof itemListSchema>;

export type CreateItemRequest = {
  name: string;
  url?: string | null;
  priceAmount?: number | null;
  priceCurrency?: string | null;
  priority: number;
  notes?: string | null;
};

export type UpdateItemRequest = {
  name?: string | null;
  url?: string | null;
  priceAmount?: number | null;
  priceCurrency?: string | null;
  priority?: number | null;
  notes?: string | null;
};

export type PublicWishlist = z.infer<typeof publicWishlistSchema>;

export type CreateThemeRequest = {
  name: string;
  tokens: ThemeTokensPatch;
};

export type UpdateThemeRequest = {
  name?: string;
  tokens?: ThemeTokensPatch;
};

export class ApiError extends Error {
  public readonly status: number;
  public readonly payload: unknown;

  constructor(status: number, message: string, payload: unknown) {
    super(message);
    this.status = status;
    this.payload = payload;
  }
}

type RequestOptions = {
  auth?: boolean;
  retry401?: boolean;
};

type AuthListener = (tokens: StoredAuthTokens | null) => void;
type AuthFailureListener = () => void;

function normalizeTokens(payload: AuthTokensResponse, fallbackRefreshToken: string | null = null): StoredAuthTokens {
  return {
    accessToken: payload.accessToken,
    accessTokenExpiresAtUtc: payload.accessTokenExpiresAtUtc,
    refreshToken: payload.refreshToken ?? fallbackRefreshToken,
    refreshTokenExpiresAtUtc: payload.refreshTokenExpiresAtUtc
  };
}

function withQuery(path: string, params: Record<string, string | number | null | undefined>): string {
  const search = new URLSearchParams();

  Object.entries(params).forEach(([key, value]) => {
    if (value === undefined || value === null || value === "") {
      return;
    }

    search.set(key, String(value));
  });

  const query = search.toString();
  return query ? `${path}?${query}` : path;
}

async function tryParseJson(response: Response): Promise<unknown> {
  const text = await response.text();

  if (!text) {
    return null;
  }

  try {
    return JSON.parse(text);
  } catch {
    return text;
  }
}

class ApiClient {
  private readonly baseUrl: string;
  private authTokens: StoredAuthTokens | null = null;
  private refreshInFlight: Promise<void> | null = null;
  private readonly authListeners = new Set<AuthListener>();
  private readonly authFailureListeners = new Set<AuthFailureListener>();

  constructor(baseUrl: string) {
    this.baseUrl = baseUrl;
  }

  getTokens(): StoredAuthTokens | null {
    return this.authTokens;
  }

  setTokens(tokens: StoredAuthTokens | null): void {
    this.authTokens = tokens;
    this.authListeners.forEach((listener) => listener(tokens));
  }

  clearTokens(): void {
    this.setTokens(null);
  }

  subscribeAuth(listener: AuthListener): () => void {
    this.authListeners.add(listener);
    return () => {
      this.authListeners.delete(listener);
    };
  }

  onAuthFailure(listener: AuthFailureListener): () => void {
    this.authFailureListeners.add(listener);
    return () => {
      this.authFailureListeners.delete(listener);
    };
  }

  private emitAuthFailure(): void {
    this.authFailureListeners.forEach((listener) => listener());
  }

  private async request<T>(
    path: string,
    init: RequestInit,
    schema: z.ZodType<T> | null,
    options: RequestOptions = {}
  ): Promise<T> {
    const auth = options.auth ?? true;
    const retry401 = options.retry401 ?? true;

    const headers = new Headers(init.headers ?? {});
    headers.set("Accept", "application/json");

    if (init.body && !headers.has("Content-Type")) {
      headers.set("Content-Type", "application/json");
    }

    if (auth && this.authTokens?.accessToken) {
      headers.set("Authorization", `Bearer ${this.authTokens.accessToken}`);
    }

    const response = await fetch(`${this.baseUrl}${path}`, {
      ...init,
      headers,
      credentials: "include"
    });

    if (response.status === 401 && auth && retry401) {
      try {
        await this.refreshAccessToken();
      } catch (error) {
        this.clearTokens();
        this.emitAuthFailure();
        throw error;
      }

      return this.request(path, init, schema, {
        ...options,
        retry401: false
      });
    }

    if (!response.ok) {
      const payload = await tryParseJson(response);
      const fallbackMessage = typeof payload === "object" && payload !== null && "title" in payload
        ? String((payload as { title: unknown }).title)
        : `Request failed with status ${response.status}`;

      throw new ApiError(response.status, fallbackMessage, payload);
    }

    if (response.status === 204 || schema === null) {
      return undefined as T;
    }

    const payload = await tryParseJson(response);
    return schema.parse(payload);
  }

  private async refreshAccessToken(): Promise<void> {
    if (this.refreshInFlight) {
      return this.refreshInFlight;
    }

    this.refreshInFlight = (async () => {
      const current = this.authTokens;
      const payload = await this.request(
        "/auth/refresh",
        {
          method: "POST",
          body: JSON.stringify({
            refreshToken: current?.refreshToken ?? null
          })
        },
        authTokensSchema,
        {
          auth: false,
          retry401: false
        }
      );

      this.setTokens(normalizeTokens(payload, current?.refreshToken ?? null));
    })();

    try {
      await this.refreshInFlight;
    } finally {
      this.refreshInFlight = null;
    }
  }

  async register(request: RegisterRequest): Promise<z.infer<typeof registerResponseSchema>> {
    return this.request(
      "/auth/register",
      {
        method: "POST",
        body: JSON.stringify(request)
      },
      registerResponseSchema,
      {
        auth: false,
        retry401: false
      }
    );
  }

  async login(request: LoginRequest): Promise<StoredAuthTokens> {
    const payload = await this.request(
      "/auth/login",
      {
        method: "POST",
        body: JSON.stringify(request)
      },
      authTokensSchema,
      {
        auth: false,
        retry401: false
      }
    );

    const tokens = normalizeTokens(payload, this.authTokens?.refreshToken ?? null);
    this.setTokens(tokens);
    return tokens;
  }

  async bootstrapRefresh(): Promise<void> {
    await this.refreshAccessToken();
  }

  async logout(): Promise<void> {
    const refreshToken = this.authTokens?.refreshToken ?? null;

    try {
      await this.request(
        "/auth/logout",
        {
          method: "POST",
          body: JSON.stringify({ refreshToken })
        },
        null,
        {
          auth: false,
          retry401: false
        }
      );
    } finally {
      this.clearTokens();
    }
  }

  async listWishlists(cursor?: string, limit = 24): Promise<WishlistListResult> {
    return this.request(withQuery("/wishlists", { cursor, limit }), { method: "GET" }, wishlistListSchema);
  }

  async getWishlist(wishlistId: string): Promise<Wishlist> {
    return this.request(`/wishlists/${wishlistId}`, { method: "GET" }, wishlistSchema);
  }

  async createWishlist(request: CreateWishlistRequest): Promise<Wishlist> {
    return this.request(
      "/wishlists",
      {
        method: "POST",
        body: JSON.stringify(request)
      },
      wishlistSchema
    );
  }

  async patchWishlist(wishlistId: string, request: UpdateWishlistRequest): Promise<Wishlist> {
    return this.request(
      `/wishlists/${wishlistId}`,
      {
        method: "PATCH",
        body: JSON.stringify(request)
      },
      wishlistSchema
    );
  }

  async deleteWishlist(wishlistId: string): Promise<void> {
    await this.request(`/wishlists/${wishlistId}`, { method: "DELETE" }, null);
  }

  async listItems(wishlistId: string, cursor?: string, limit = 50): Promise<ItemListResult> {
    return this.request(
      withQuery(`/wishlists/${wishlistId}/items`, { cursor, limit }),
      { method: "GET" },
      itemListSchema
    );
  }

  async createItem(wishlistId: string, request: CreateItemRequest): Promise<Item> {
    return this.request(
      `/wishlists/${wishlistId}/items`,
      {
        method: "POST",
        body: JSON.stringify(request)
      },
      itemSchema
    );
  }

  async patchItem(wishlistId: string, itemId: number, request: UpdateItemRequest): Promise<Item> {
    return this.request(
      `/wishlists/${wishlistId}/items/${itemId}`,
      {
        method: "PATCH",
        body: JSON.stringify(request)
      },
      itemSchema
    );
  }

  async deleteItem(wishlistId: string, itemId: number): Promise<void> {
    await this.request(`/wishlists/${wishlistId}/items/${itemId}`, { method: "DELETE" }, null);
  }

  async rotateShareLink(wishlistId: string): Promise<z.infer<typeof shareRotationSchema>> {
    return this.request(
      `/wishlists/${wishlistId}/share`,
      {
        method: "POST"
      },
      shareRotationSchema
    );
  }

  async disableShareLink(wishlistId: string): Promise<void> {
    await this.request(`/wishlists/${wishlistId}/share`, { method: "DELETE" }, null);
  }

  async getDefaultTheme(): Promise<ThemePreset> {
    return this.request(
      "/themes/default",
      { method: "GET" },
      defaultThemeSchema,
      {
        auth: false,
        retry401: false
      }
    );
  }

  async listThemes(cursor?: string, limit = 50): Promise<{ items: Theme[]; nextCursor: string | null }> {
    return this.request(withQuery("/themes", { cursor, limit }), { method: "GET" }, themeListSchema);
  }

  async getTheme(themeId: string): Promise<Theme> {
    return this.request(`/themes/${themeId}`, { method: "GET" }, themeSchema);
  }

  async createTheme(request: CreateThemeRequest): Promise<Theme> {
    return this.request(
      "/themes",
      {
        method: "POST",
        body: JSON.stringify(request)
      },
      themeSchema
    );
  }

  async patchTheme(themeId: string, request: UpdateThemeRequest): Promise<Theme> {
    return this.request(
      `/themes/${themeId}`,
      {
        method: "PATCH",
        body: JSON.stringify(request)
      },
      themeSchema
    );
  }

  async deleteTheme(themeId: string): Promise<void> {
    await this.request(`/themes/${themeId}`, { method: "DELETE" }, null);
  }

  async getPublicWishlist(token: string, cursor?: string, limit = 100): Promise<PublicWishlist> {
    return this.request(
      withQuery(`/public/wishlists/${token}`, { cursor, limit }),
      { method: "GET" },
      publicWishlistSchema,
      {
        auth: false,
        retry401: false
      }
    );
  }
}

const apiBaseUrl = "/api";

export const apiClient = new ApiClient(apiBaseUrl);
