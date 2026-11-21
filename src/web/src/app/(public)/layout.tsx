import React from "react";
import { PublicHeader } from "@/features/verification/components/public-header";
import { uiStyles } from "@/styles/ui.styles";

export default function PublicLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  return (
    <div className="min-h-[100dvh] flex flex-col">
      <PublicHeader />
      <main className={uiStyles.layout.publicContainer}>{children}</main>
    </div>
  );
}
