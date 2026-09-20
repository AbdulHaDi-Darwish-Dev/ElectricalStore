export {
  AUTH_REFRESH_COOKIE_NAME,
  AUTH_REFRESH_COOKIE_PATH,
  authRefreshCookieOptions,
  clearAuthRefreshCookieOptions,
  maxAgeSecondsFromExpiresAtUtc,
} from "./refresh-cookie";
export {
  LOCAL_DEV_LOGIN_CLIENT_IP,
  resolveForwardableClientIp,
  resolveLocalDevLoginClientIp,
} from "./client-ip";
export { isSafeReturnTo, resolveSafeReturnTo } from "./return-to";
export { hasAnyPermission, hasPermission } from "./permissions";
export { getAuthErrorMessage, MFA_UNAVAILABLE_MESSAGE } from "./errors";
export {
  useAuthStore,
  selectAuthReady,
  selectIsAuthenticated,
  type AuthStatus,
} from "./session-store";
export { authenticatedFetch, type AuthenticatedFetchOptions } from "./authenticated-fetch";
export {
  coordinatedRefresh,
  hasRefreshInFlight,
  postLogin,
  postLogout,
  fetchMe,
  broadcastLogout,
} from "./auth-api";
export { AuthProvider, useAuthActions } from "./auth-provider";
export type {
  AuthenticationResultDto,
  BrowserAuthTokensDto,
  BrowserLoginResponseDto,
  LoginRequest,
  MeDto,
  RegisterRequest,
  RegisterResponseDto,
} from "./types";
export {
  isAuthenticationResult,
  isMfaLoginChallenge,
  toBrowserAuthTokens,
} from "./types";
