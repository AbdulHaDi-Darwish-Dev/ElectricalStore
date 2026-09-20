import { adminQueryKey } from "@/features/admin";

export const adminSettingsDomain = "settings" as const;

export const adminSettingsKeys = {
  all: () => adminQueryKey(adminSettingsDomain),
  ordering: () => adminQueryKey(adminSettingsDomain, "ordering"),
} as const;
