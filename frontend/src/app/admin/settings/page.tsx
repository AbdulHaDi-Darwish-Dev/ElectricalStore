import type { Metadata } from "next";
import { SettingsView } from "@/components/admin-settings";

export const metadata: Metadata = {
  title: "الإعدادات",
  robots: { index: false, follow: false },
};

export default function AdminSettingsPage() {
  return <SettingsView />;
}
