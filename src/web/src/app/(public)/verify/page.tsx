"use client";

import React, { useState } from "react";
import { WarningCircle } from "@phosphor-icons/react";
import { DoubleBezelCard } from "@/components/ui/double-bezel-card";
import { DocumentCodeForm } from "@/features/verification/components/document-code-form";
import { JwsInputPanel } from "@/features/verification/components/jws-input-panel";
import { PdfDropzone } from "@/features/verification/components/pdf-dropzone";
import { VerificationResultCard } from "@/features/verification/components/verification-result-card";
import {
  VerifyModeTabs,
  type VerifyMode,
} from "@/features/verification/components/verify-mode-tabs";
import { useVerification } from "@/features/verification/hooks/use-verification";
import { mapErrorMessage } from "@/i18n/map-error-message";
import { useTranslation } from "@/i18n/language-context";
import { uiStyles } from "@/styles/ui.styles";

export default function VerifyPage() {
  const { t } = useTranslation();
  const [mode, setMode] = useState<VerifyMode>("pdf");
  const { phase, result, error, isChecking, verifyPdf, verifyJws, reset } =
    useVerification();

  const handleModeChange = (nextMode: VerifyMode) => {
    setMode(nextMode);
    reset();
  };

  return (
    <DoubleBezelCard>
      <div className="space-y-6">
        <div className="space-y-2">
          <span className={uiStyles.typography.eyebrowBadge}>
            {t.verify.page.eyebrow}
          </span>
          <h1 className={uiStyles.typography.sectionTitle}>
            {t.verify.page.title}
          </h1>
          <p className={uiStyles.typography.bodyMuted}>
            {t.verify.page.subtitle}
          </p>
        </div>

        <VerifyModeTabs mode={mode} onChange={handleModeChange} />

        {mode === "pdf" ? (
          <PdfDropzone
            isChecking={isChecking}
            onSelectFile={(file) => void verifyPdf(file)}
          />
        ) : (
          <JwsInputPanel
            isChecking={isChecking}
            onSubmitJws={(token) => void verifyJws(token)}
          />
        )}

        {phase === "error" && (
          <div className={uiStyles.alert.error} role="alert">
            <WarningCircle size={18} weight="fill" className="shrink-0 mt-0.5" />
            <span>{mapErrorMessage(error, t.errors)}</span>
          </div>
        )}

        {result && <VerificationResultCard result={result} onReset={reset} />}

        <div className="pt-4 border-t border-white/[0.06]">
          <DocumentCodeForm />
        </div>
      </div>
    </DoubleBezelCard>
  );
}
