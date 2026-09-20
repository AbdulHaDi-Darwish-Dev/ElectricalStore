import type { Metadata } from "next";
import { IamRolesListView } from "@/components/admin-iam";

export const metadata: Metadata = {
  title: "الأدوار",
  robots: { index: false, follow: false },
};

export default function AdminIamRolesPage() {
  return <IamRolesListView />;
}
