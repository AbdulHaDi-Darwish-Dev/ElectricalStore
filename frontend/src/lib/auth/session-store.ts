"use client";

import { create } from "zustand";
import type { BrowserAuthTokensDto, MeDto } from "./types";

export type AuthStatus = "initializing" | "authenticated" | "anonymous";

type AuthSessionState = {
  status: AuthStatus;
  accessToken: string | null;
  accessTokenExpiresAtUtc: string | null;
  userId: string | null;
  permissions: string[];
  setInitializing: () => void;
  applyTokens: (tokens: BrowserAuthTokensDto) => void;
  applyMe: (me: MeDto) => void;
  setAuthenticated: (tokens: BrowserAuthTokensDto, me: MeDto) => void;
  clearSession: () => void;
  markAnonymous: () => void;
};

/**
 * In-memory auth session only — never persist.
 * Refresh token lives in HttpOnly cookie, not here.
 */
export const useAuthStore = create<AuthSessionState>((set) => ({
  status: "initializing",
  accessToken: null,
  accessTokenExpiresAtUtc: null,
  userId: null,
  permissions: [],

  setInitializing: () =>
    set({
      status: "initializing",
    }),

  applyTokens: (tokens) =>
    set({
      accessToken: tokens.accessToken,
      accessTokenExpiresAtUtc: tokens.accessTokenExpiresAtUtc,
      userId: tokens.userId,
    }),

  applyMe: (me) =>
    set({
      userId: me.userId,
      permissions: me.permissions ?? [],
    }),

  setAuthenticated: (tokens, me) =>
    set({
      status: "authenticated",
      accessToken: tokens.accessToken,
      accessTokenExpiresAtUtc: tokens.accessTokenExpiresAtUtc,
      userId: me.userId,
      permissions: me.permissions ?? [],
    }),

  clearSession: () =>
    set({
      status: "anonymous",
      accessToken: null,
      accessTokenExpiresAtUtc: null,
      userId: null,
      permissions: [],
    }),

  markAnonymous: () =>
    set({
      status: "anonymous",
      accessToken: null,
      accessTokenExpiresAtUtc: null,
      userId: null,
      permissions: [],
    }),
}));

export function selectIsAuthenticated(state: AuthSessionState): boolean {
  return state.status === "authenticated" && Boolean(state.accessToken);
}

export function selectAuthReady(state: AuthSessionState): boolean {
  return state.status !== "initializing";
}
