import { fetchMe } from "@/lib/auth";
import { useAuthStore } from "@/lib/auth";

/**
 * After IAM mutations that may affect the current actor, refresh `/me`
 * permissions in memory. Access token is unchanged; permissions may change.
 */
export async function refreshCurrentUserPermissions(): Promise<void> {
  const token = useAuthStore.getState().accessToken;
  if (!token) return;
  const me = await fetchMe(token);
  useAuthStore.getState().applyMe(me);
}
