import type { Metadata } from "next";
import { AccessLandingView } from "@/components/admin-iam";

export const metadata: Metadata = {
  title: "إدارة الوصول",
  robots: { index: false, follow: false },
};

export default function AdminAccessPage() {
  return <AccessLandingView />;
}
