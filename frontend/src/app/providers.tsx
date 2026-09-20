"use client";

import { QueryClientProvider } from "@tanstack/react-query";
import { useState, type ReactNode } from "react";
import { createQueryClient } from "@/lib/query/query-client";

type ProvidersProps = {
  children: ReactNode;
};

/**
 * Client providers for interactive areas (admin / account).
 * TanStack Query only — no auth session provider in F1.
 */
export function Providers({ children }: ProvidersProps) {
  const [queryClient] = useState(() => createQueryClient());

  return (
    <QueryClientProvider client={queryClient}>{children}</QueryClientProvider>
  );
}
