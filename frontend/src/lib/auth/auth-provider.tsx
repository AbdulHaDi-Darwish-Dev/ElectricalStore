"use client";

import {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useRef,
  type ReactNode,
} from "react";
import { useQueryClient } from "@tanstack/react-query";
import {
  broadcastLogout,
  broadcastSession,
  coordinatedRefresh,
  fetchMe,
  getAuthBroadcastChannel,
  postLogout,
  type AuthChannelMessage,
} from "./auth-api";
import { useAuthStore } from "./session-store";
import type { BrowserAuthTokensDto, MeDto } from "./types";

const PROACTIVE_REFRESH_SKEW_MS = 60_000;

type AuthContextValue = {
  loginWithTokens: (tokens: BrowserAuthTokensDto) => Promise<void>;
  logout: () => Promise<void>;
};

const AuthContext = createContext<AuthContextValue | null>(null);

export function AuthProvider({ children }: { children: ReactNode }) {
  const queryClient = useQueryClient();
  const proactiveTimer = useRef<ReturnType<typeof setTimeout> | null>(null);
  const accessTokenExpiresAtUtc = useAuthStore((s) => s.accessTokenExpiresAtUtc);
  const status = useAuthStore((s) => s.status);

  const clearProactive = useCallback(() => {
    if (proactiveTimer.current) {
      clearTimeout(proactiveTimer.current);
      proactiveTimer.current = null;
    }
  }, []);

  const applyAuthenticated = useCallback(
    async (tokens: BrowserAuthTokensDto) => {
      const me = await fetchMe(tokens.accessToken);
      useAuthStore.getState().setAuthenticated(tokens, me);
      broadcastSession(tokens, me);
      return me;
    },
    [],
  );

  useEffect(() => {
    let cancelled = false;

    async function bootstrap() {
      useAuthStore.getState().setInitializing();
      try {
        const tokens = await coordinatedRefresh();
        if (cancelled) return;
        if (!tokens) {
          useAuthStore.getState().markAnonymous();
          return;
        }
        await applyAuthenticated(tokens);
      } catch {
        if (!cancelled) {
          useAuthStore.getState().markAnonymous();
        }
      }
    }

    void bootstrap();

    const channel = getAuthBroadcastChannel();
    const onMessage = (event: MessageEvent<AuthChannelMessage>) => {
      const data = event.data;
      if (!data || typeof data !== "object") return;
      if (data.type === "logout") {
        clearProactive();
        useAuthStore.getState().clearSession();
        void queryClient.removeQueries({ queryKey: ["my-orders"] });
        void queryClient.removeQueries({ queryKey: ["my-order"] });
        return;
      }
      if (data.type === "session") {
        useAuthStore.getState().setAuthenticated(data.tokens, data.me);
      }
    };
    channel?.addEventListener("message", onMessage);

    return () => {
      cancelled = true;
      clearProactive();
      channel?.removeEventListener("message", onMessage);
    };
  }, [applyAuthenticated, clearProactive, queryClient]);

  useEffect(() => {
    clearProactive();
    if (status !== "authenticated" || !accessTokenExpiresAtUtc) {
      return;
    }

    const expiresMs = Date.parse(accessTokenExpiresAtUtc);
    if (Number.isNaN(expiresMs)) {
      return;
    }

    const delay = Math.max(
      5_000,
      expiresMs - Date.now() - PROACTIVE_REFRESH_SKEW_MS,
    );

    proactiveTimer.current = setTimeout(() => {
      void (async () => {
        const tokens = await coordinatedRefresh();
        if (!tokens) {
          useAuthStore.getState().clearSession();
          return;
        }
        try {
          await applyAuthenticated(tokens);
        } catch {
          useAuthStore.getState().clearSession();
        }
      })();
    }, delay);

    return () => {
      clearProactive();
    };
  }, [accessTokenExpiresAtUtc, status, applyAuthenticated, clearProactive]);

  const value: AuthContextValue = {
    loginWithTokens: async (tokens) => {
      await applyAuthenticated(tokens);
    },
    logout: async () => {
      clearProactive();
      await postLogout();
      useAuthStore.getState().clearSession();
      broadcastLogout();
      void queryClient.removeQueries({ queryKey: ["my-orders"] });
      void queryClient.removeQueries({ queryKey: ["my-order"] });
    },
  };

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuthActions(): AuthContextValue {
  const ctx = useContext(AuthContext);
  if (!ctx) {
    throw new Error("useAuthActions must be used within AuthProvider");
  }
  return ctx;
}

export type { MeDto };
