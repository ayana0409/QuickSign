import React from "react";
import { DocumentVerificationsView } from "@/features/documents/components/document-verifications-view";

export default async function DocumentVerificationsPage({
  params,
}: {
  params: Promise<{ id: string }>;
}) {
  const { id } = await params;
  return <DocumentVerificationsView documentId={decodeURIComponent(id)} />;
}
