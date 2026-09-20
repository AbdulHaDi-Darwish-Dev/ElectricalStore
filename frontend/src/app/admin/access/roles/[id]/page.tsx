import type { Metadata } from "next";
import { IamRoleDetailView } from "@/components/admin-iam";

export const metadata: Metadata = {
  title: "تفاصيل الدور",
  robots: { index: false, follow: false },
};

type Props = {
  params: Promise<{ id: string }>;
};

export default async function AdminIamRoleDetailPage({ params }: Props) {
  const { id } = await params;
  return <IamRoleDetailView roleId={id} />;
}
