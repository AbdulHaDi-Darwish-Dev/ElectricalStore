import type { Metadata } from "next";
import { IamUsersListView } from "@/components/admin-iam";

export const metadata: Metadata = {
  title: "المستخدمون",
  robots: { index: false, follow: false },
};

export default function AdminIamUsersPage() {
  return <IamUsersListView />;
}
