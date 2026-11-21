import type { Metadata } from "next";
import { QrLookupView } from "@/features/verification/components/qr-lookup-view";

export const metadata: Metadata = {
  robots: {
    index: false,
    follow: false,
  },
};

export default async function VerifyByCodePage({
  params,
}: {
  params: Promise<{ documentCode: string }>;
}) {
  const { documentCode } = await params;
  return <QrLookupView rawCode={decodeURIComponent(documentCode)} />;
}
