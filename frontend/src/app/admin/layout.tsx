import type { ReactNode } from "react";
import { AdminSidebar } from "@/components/shared/admin-sidebar";
import { AdminTopBar } from "@/components/shared/admin-topbar";

export default function AdminLayout({
  children,
}: Readonly<{
  children: ReactNode;
}>) {
  return (
    <div className="flex min-h-full flex-col md:flex-row">
      <AdminSidebar />
      <div className="flex min-w-0 flex-1 flex-col">
        <AdminTopBar />
        <main className="flex-1 px-4 py-6 sm:px-6">{children}</main>
      </div>
    </div>
  );
}
