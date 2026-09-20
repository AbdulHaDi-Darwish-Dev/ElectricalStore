import type { Metadata } from "next";
import { IamUserDetailView } from "@/components/admin-iam";

export const metadata: Metadata = {
  title: "تفاصيل المستخدم",
  robots: { index: false, follow: false },
};

type Props = {
  params: Promise<{ id: string }>;
};

export default async function AdminIamUserDetailPage({ params }: Props) {
  const { id } = await params;
  return <IamUserDetailView userId={id} />;
}
