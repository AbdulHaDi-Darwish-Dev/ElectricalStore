import { QueryClient } from "@tanstack/react-query";

/**
 * Browser QueryClient factory.
 * Conservative defaults — no aggressive refetch; Storefront catalog remains server-first.
 */
export function createQueryClient() {
  return new QueryClient({
    defaultOptions: {
      queries: {
        staleTime: 30_000,
        refetchOnWindowFocus: false,
        retry: 1,
      },
      mutations: {
        retry: 0,
      },
    },
  });
}
