"use client";

import React, { useEffect, useState } from "react";
import Link from "next/link";
import { ArrowLeft, CircleNotch, WarningCircle } from "@phosphor-icons/react";
import { DoubleBezelCard } from "@/components/ui/double-bezel-card";
import { JwsInputPanel } from "@/features/verification/components/jws-input-panel";
import { PdfDropzone } from "@/features/verification/components/pdf-dropzone";
import { SecurityNoticeBanner } from "@/features/verification/components/security-notice-banner";
import { VerificationResultCard } from "@/features/verification/components/verification-result-card";
import {
  VerifyModeTabs,
  type VerifyMode,
} from "@/features/verification/components/verify-mode-tabs";
import { useVerification } from "@/features/verification/hooks/use-verification";
import {
  isValidDocumentCode,
  normalizeDocumentCode,
} from "@/features/verification/utils/document-code";
import { detectCodeMismatch } from "@/features/verification/utils/verification-view-model";
import { mapErrorMessage } from "@/i18n/map-error-message";
import { useTranslation } from "@/i18n/language-context";
import { uiStyles } from "@/styles/ui.styles";

export function QrLookupView({ rawCode }: { rawCode: string }) {
  const { t } = useTranslation();
  const normalizedCode = normalizeDocumentCode(rawCode);
  const codeValid = isValidDocumentCode(normalizedCode);

  const codeLookup = useVerification();
  const fileVerify = useVerification();
  const [mode, setMode] = useState<VerifyMode>("pdf");

  useEffect(() => {
    if (codeValid) {
      void codeLookup.lookupCode(normalizedCode);
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [normalizedCode, codeValid]);

  const hasMismatch =
    fileVerify.result !== null &&
    detectCodeMismatch(normalizedCode, fileVerify.result);

  return (
    <div className="space-y-6">
      <Link href="/verify" className={uiStyles.button.ghostPill}>
        <ArrowLeft size={15} weight="light" />
        <span>{t.verify.qrPage.backToVerify}</span>
      </Link>

      <DoubleBezelCard>
        <div className="space-y-6">
          <div className="space-y-2">
            <span className={uiStyles.typography.eyebrowBadge}>
              {t.verify.qrPage.eyebrow}
            </span>
            <h1 className={uiStyles.typography.sectionTitle}>
              {t.verify.qrPage.title}:{" "}
              <span className="font-mono text-emerald-400">{normalizedCode}</span>
            </h1>
            <p className={uiStyles.typography.bodyMuted}>
              {t.verify.qrPage.subtitle}
            </p>
          </div>

          {!codeValid && (
            <div className={uiStyles.alert.error} role="alert">
              <WarningCircle size={18} weight="fill" className="shrink-0 mt-0.5" />
              <span>{t.verify.guards.INVALID_DOCUMENT_CODE}</span>
            </div>
          )}

          {codeValid && codeLookup.isChecking && (
            <div className="flex items-center gap-3 py-6 text-sm text-zinc-400">
              <CircleNotch size={20} weight="light" className="animate-spin text-emerald-400" />
              <span>{t.common.loading}</span>
            </div>
          )}

          {codeValid && codeLookup.phase === "error" && (
            <div className={uiStyles.alert.error} role="alert">
              <WarningCircle size={18} weight="fill" className="shrink-0 mt-0.5" />
              <span>{mapErrorMessage(codeLookup.error, t.errors)}</span>
            </div>
          )}

          {codeLookup.result && (
            <>
              <SecurityNoticeBanner />
              <VerificationResultCard result={codeLookup.result} />

              <div className="pt-4 border-t border-white/[0.06] space-y-4">
                <div className="space-y-1">
                  <h2 className="text-base font-semibold text-white">
                    {t.verify.qrPage.uploadSectionTitle}
                  </h2>
                  <p className="text-xs sm:text-sm text-zinc-400">
                    {t.verify.qrPage.uploadSectionHint}
                  </p>
                </div>

                <VerifyModeTabs mode={mode} onChange={setMode} />

                {mode === "pdf" ? (
                  <PdfDropzone
                    isChecking={fileVerify.isChecking}
                    onSelectFile={(file) => void fileVerify.verifyPdf(file)}
                  />
                ) : (
                  <JwsInputPanel
                    isChecking={fileVerify.isChecking}
                    onSubmitJws={(token) => void fileVerify.verifyJws(token)}
                  />
                )}

                {fileVerify.phase === "error" && (
                  <div className={uiStyles.alert.error} role="alert">
                    <WarningCircle size={18} weight="fill" className="shrink-0 mt-0.5" />
                    <span>{mapErrorMessage(fileVerify.error, t.errors)}</span>
                  </div>
                )}

                {hasMismatch && (
                  <div className={uiStyles.alert.error} role="alert">
                    <WarningCircle size={18} weight="fill" className="shrink-0 mt-0.5" />
                    <span>{t.verify.qrPage.mismatchAlert}</span>
                  </div>
                )}

                {fileVerify.result && (
                  <VerificationResultCard
                    result={fileVerify.result}
                    onReset={fileVerify.reset}
                  />
                )}
              </div>
            </>
          )}
        </div>
      </DoubleBezelCard>
    </div>
  );
}
