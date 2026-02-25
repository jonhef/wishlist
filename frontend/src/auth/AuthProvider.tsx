import { createContext, useContext, useEffect, useMemo, useRef, useState } from "react";
import type { ReactNode } from "react";
import { apiClient, type StoredAuthTokens } from "../api/client";

type AuthSnapshot = {
  email: string | null;
  tokens: StoredAuthTokens | null;
};

type AuthContextValue = {
  email: string | null;
  tokens: StoredAuthTokens | null;
  isAuthenticated: boolean;
  isInitializing: boolean;
  login: (email: string, password: string) => Promise<void>;
  registerAndLogin: (email: string, password: string) => Promise<void>;
  logout: () => Promise<void>;
};

const AUTH_STORAGE_KEY = "wishlist.auth";

const AuthContext = createContext<AuthContextValue | undefined>(undefined);

function readStoredAuth(): AuthSnapshot {
  const raw = localStorage.getItem(AUTH_STORAGE_KEY);

  if (!raw) {
    return { email: null, tokens: null };
  }

  try {
    const parsed = JSON.parse(raw) as AuthSnapshot;

    if (!parsed.tokens?.accessToken) {
      return { email: parsed.email ?? null, tokens: null };
    }

    return {
      email: parsed.email ?? null,
      tokens: parsed.tokens
    };
  } catch {
    return { email: null, tokens: null };
  }
}

function persistAuth(snapshot: AuthSnapshot): void {
  if (!snapshot.tokens) {
    localStorage.removeItem(AUTH_STORAGE_KEY);
    return;
  }

  localStorage.setItem(AUTH_STORAGE_KEY, JSON.stringify(snapshot));
}

export function AuthProvider({ children }: { children: ReactNode }): JSX.Element {
  const [email, setEmail] = useState<string | null>(null);
  const [tokens, setTokens] = useState<StoredAuthTokens | null>(null);
  const [isInitializing, setIsInitializing] = useState(true);
  const emailRef = useRef<string | null>(null);

  useEffect(() => {
    emailRef.current = email;
  }, [email]);

  useEffect(() => {
    const stored = readStoredAuth();
    emailRef.current = stored.email;
    setEmail(stored.email);
    setTokens(stored.tokens);
    apiClient.setTokens(stored.tokens);

    const authSubscription = apiClient.subscribeAuth((nextTokens) => {
      setTokens(nextTokens);
      persistAuth({
        email: emailRef.current,
        tokens: nextTokens
      });
    });

    const failureSubscription = apiClient.onAuthFailure(() => {
      setTokens(null);
      setEmail(null);
      persistAuth({ email: null, tokens: null });
    });

    const bootstrap = async (): Promise<void> => {
      try {
        await apiClient.bootstrapRefresh();
      } catch {
        // Ignore: user is not authenticated yet.
      } finally {
        setIsInitializing(false);
      }
    };

    void bootstrap();

    return () => {
      authSubscription();
      failureSubscription();
    };
  }, []);

  const value = useMemo<AuthContextValue>(() => ({
    email,
    tokens,
    isInitializing,
    isAuthenticated: Boolean(tokens?.accessToken),
    login: async (nextEmail: string, password: string) => {
      const nextTokens = await apiClient.login({
        email: nextEmail,
        password
      });

      setEmail(nextEmail);
      setTokens(nextTokens);
      persistAuth({
        email: nextEmail,
        tokens: nextTokens
      });
    },
    registerAndLogin: async (nextEmail: string, password: string) => {
      await apiClient.register({
        email: nextEmail,
        password
      });
      const nextTokens = await apiClient.login({
        email: nextEmail,
        password
      });

      setEmail(nextEmail);
      setTokens(nextTokens);
      persistAuth({
        email: nextEmail,
        tokens: nextTokens
      });
    },
    logout: async () => {
      await apiClient.logout();
      setEmail(null);
      setTokens(null);
      persistAuth({
        email: null,
        tokens: null
      });
    }
  }), [email, isInitializing, tokens]);

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth(): AuthContextValue {
  const context = useContext(AuthContext);

  if (!context) {
    throw new Error("useAuth must be used inside AuthProvider");
  }

  return context;
}
