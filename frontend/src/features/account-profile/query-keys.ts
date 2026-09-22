export const accountProfileKeys = {
  all: ["account-profile"] as const,
  detail: () => [...accountProfileKeys.all, "detail"] as const,
};
