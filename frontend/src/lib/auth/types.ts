/**
 * Auth DTOs — match ASP.NET / Permixa camelCase JSON. No invented fields.
 */

export type AuthenticationResultDto = {
  userId: string;
  accessToken: string;
  accessTokenExpiresAtUtc: string;
  refreshToken: string;
  refreshTokenExpiresAtUtc: string;
};

export type MfaLoginChallengeDto = {
  mfaProof: string;
  expiresAtUtc: string;
};

/** Browser-visible login success (refreshToken stripped). */
export type BrowserAuthTokensDto = {
  userId: string;
  accessToken: string;
  accessTokenExpiresAtUtc: string;
};

export type BrowserLoginSuccessDto = {
  kind: "authenticated";
} & BrowserAuthTokensDto;

export type BrowserLoginMfaDto = {
  kind: "mfaRequired";
  /** Opaque; do not treat as authenticated. Expiry for UX only. */
  expiresAtUtc?: string;
};

export type BrowserLoginResponseDto = BrowserLoginSuccessDto | BrowserLoginMfaDto;

export type MeDto = {
  userId: string;
  permissions: string[];
};

export type RegisterRequest = {
  userName: string;
  email: string;
  password: string;
};

export type RegisterResponseDto = {
  userId: string;
  userName: string;
  email: string;
  emailConfirmed: boolean;
};

export type LoginRequest = {
  emailOrUserName: string;
  password: string;
};

export function isAuthenticationResult(value: unknown): value is AuthenticationResultDto {
  if (typeof value !== "object" || value === null) {
    return false;
  }
  const v = value as Record<string, unknown>;
  return (
    typeof v.userId === "string" &&
    typeof v.accessToken === "string" &&
    typeof v.accessTokenExpiresAtUtc === "string" &&
    typeof v.refreshToken === "string" &&
    typeof v.refreshTokenExpiresAtUtc === "string"
  );
}

export function isMfaLoginChallenge(value: unknown): value is MfaLoginChallengeDto {
  if (typeof value !== "object" || value === null) {
    return false;
  }
  const v = value as Record<string, unknown>;
  return typeof v.mfaProof === "string" && typeof v.expiresAtUtc === "string";
}

export function toBrowserAuthTokens(
  auth: AuthenticationResultDto,
): BrowserAuthTokensDto {
  return {
    userId: auth.userId,
    accessToken: auth.accessToken,
    accessTokenExpiresAtUtc: auth.accessTokenExpiresAtUtc,
  };
}
