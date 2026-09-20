import type { Metadata } from "next";
import { IamPermissionsCatalogView } from "@/components/admin-iam";

export const metadata: Metadata = {
  title: "الصلاحيات",
  robots: { index: false, follow: false },
};

export default function AdminIamPermissionsPage() {
  return <IamPermissionsCatalogView />;
}
