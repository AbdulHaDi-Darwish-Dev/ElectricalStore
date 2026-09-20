import type { Metadata } from "next";
import { IamAuditListView } from "@/components/admin-iam";

export const metadata: Metadata = {
  title: "سجل التدقيق",
  robots: { index: false, follow: false },
};

export default function AdminIamAuditPage() {
  return <IamAuditListView />;
}
