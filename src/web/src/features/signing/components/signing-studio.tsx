"use client";

import React, { useEffect, useId, useState } from "react";
import Link from "next/link";
import {
  ArrowUpRight,
  Certificate,
  CheckCircle,
  CircleNotch,
  FilePdf,
  UploadSimple,
  WarningCircle,
} from "@phosphor-icons/react";
import { clsx } from "clsx";
import { CopyButton } from "@/components/ui/copy-button";
import { DoubleBezelCard } from "@/components/ui/double-bezel-card";
import { EmptyState } from "@/components/ui/empty-state";
import { FormField } from "@/components/ui/form-field";
import { IslandButton } from "@/components/ui/island-button";
import type { CertificateResponseDto } from "@/features/certificates/types/certificate.types";
import type { SignJsonResponse } from "@/features/signing/types/signing.types";
import { validatePdfFile } from "@/features/verification/utils/file-guard";
import { mapErrorMessage } from "@/i18n/map-error-message";
import { useTranslation } from "@/i18n/language-context";
import { certificateService } from "@/services/certificate.service";
import { cryptoService } from "@/services/crypto.service";
import { uiStyles } from "@/styles/ui.styles";

type SignMode = "pdf" | "json";

export function SigningStudio() {
  const { t } = useTranslation();
  const pdfInputId = useId();

  const [certs, setCerts] = useState<CertificateResponseDto[]>([]);
  const [loadingCerts, setLoadingCerts] = useState(true);
  const [mode, setMode] = useState<SignMode>("pdf");

  const [selectedCertId, setSelectedCertId] = useState("");
  const [reason, setReason] = useState("");
  const [location, setLocation] = useState("");
  const [pdfFile, setPdfFile] = useState<File | null>(null);
  const [isDragging, setIsDragging] = useState(false);

  const [jsonText, setJsonText] = useState("");
  const [submitting, setSubmitting] = useState(false);
  const [errorMsg, setErrorMsg] = useState<string | null>(null);

  const [pdfSuccess, setPdfSuccess] = useState<{
    documentCode: string;
    fileName: string;
  } | null>(null);
  const [jsonSuccess, setJsonSuccess] = useState<SignJsonResponse | null>(null);

  useEffect(() => {
    let cancelled = false;
    (async () => {
      try {
        const list = await certificateService.getMyCertificates();
        if (!cancelled) {
          setCerts(list.filter((c) => c.status === 1));
        }
      } finally {
        if (!cancelled) {
          setLoadingCerts(false);
        }
      }
    })();
    return () => {
      cancelled = true;
    };
  }, []);

  const handleSelectPdf = async (file: File | undefined) => {
    if (!file) return;
    setErrorMsg(null);
    const guardErr = await validatePdfFile(file);
    if (guardErr) {
      setPdfFile(null);
      setErrorMsg(t.verify.guards[guardErr]);
      return;
    }
    setPdfFile(file);
  };

  const handleSignPdf = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!pdfFile) return;

    setErrorMsg(null);
    setPdfSuccess(null);
    setSubmitting(true);
    try {
      const res = await cryptoService.signPdf({
        file: pdfFile,
        certificateId: selectedCertId || undefined,
        reason: reason.trim() || undefined,
        location: location.trim() || undefined,
      });

      // Trigger automatic browser download and immediately revoke object URL
      const url = URL.createObjectURL(res.blob);
      const anchor = document.createElement("a");
      anchor.href = url;
      anchor.download = res.fileName;
      document.body.appendChild(anchor);
      anchor.click();
      anchor.remove();
      URL.revokeObjectURL(url);

      setPdfSuccess({
        documentCode: res.documentCode,
        fileName: res.fileName,
      });
      setPdfFile(null);
    } catch (err) {
      setErrorMsg(mapErrorMessage(err, t.errors));
    } finally {
      setSubmitting(false);
    }
  };

  const handleSignJson = async (e: React.FormEvent) => {
    e.preventDefault();
    setErrorMsg(null);
    setJsonSuccess(null);

    let parsed: Record<string, unknown>;
    try {
      const val = JSON.parse(jsonText) as unknown;
      if (!val || typeof val !== "object" || Array.isArray(val)) {
        setErrorMsg(t.dashboard.sign.form.invalidJson);
        return;
      }
      parsed = val as Record<string, unknown>;
    } catch {
      setErrorMsg(t.dashboard.sign.form.invalidJson);
      return;
    }

    setSubmitting(true);
    try {
      const res = await cryptoService.signJson({
        payload: parsed,
        certificateId: selectedCertId || undefined,
      });
      setJsonSuccess(res);
    } catch (err) {
      setErrorMsg(mapErrorMessage(err, t.errors));
    } finally {
      setSubmitting(false);
    }
  };

  const resetResults = () => {
    setPdfSuccess(null);
    setJsonSuccess(null);
    setErrorMsg(null);
  };

  return (
    <div className="space-y-6">
      <div className="space-y-1.5">
        <span className={uiStyles.typography.eyebrowBadge}>
          {t.dashboard.sign.eyebrow}
        </span>
        <h1 className={uiStyles.typography.sectionTitle}>
          {t.dashboard.sign.title}
        </h1>
        <p className={uiStyles.typography.bodyMuted}>
          {t.dashboard.sign.subtitle}
        </p>
      </div>

      {loadingCerts ? (
        <div className="flex items-center gap-3 py-12 text-sm text-zinc-400">
          <CircleNotch size={20} weight="light" className="animate-spin text-emerald-400" />
          <span>{t.common.loading}</span>
        </div>
      ) : certs.length === 0 ? (
        <EmptyState
          icon={<Certificate size={24} weight="light" />}
          title={t.dashboard.sign.noCertTitle}
          description={t.dashboard.sign.noCertDesc}
          action={
            <Link href="/certificates" className={uiStyles.button.ghostPill}>
              {t.dashboard.sign.goToCerts}
            </Link>
          }
        />
      ) : (
        <DoubleBezelCard>
          <div className="space-y-6">
            <div role="tablist" className={uiStyles.tabs.list}>
              <button
                type="button"
                role="tab"
                aria-selected={mode === "pdf"}
                onClick={() => {
                  setMode("pdf");
                  resetResults();
                }}
                className={clsx(
                  uiStyles.tabs.tabBase,
                  mode === "pdf" ? uiStyles.tabs.tabActive : uiStyles.tabs.tabInactive
                )}
              >
                {t.dashboard.sign.tabs.pdf}
              </button>
              <button
                type="button"
                role="tab"
                aria-selected={mode === "json"}
                onClick={() => {
                  setMode("json");
                  resetResults();
                }}
                className={clsx(
                  uiStyles.tabs.tabBase,
                  mode === "json" ? uiStyles.tabs.tabActive : uiStyles.tabs.tabInactive
                )}
              >
                {t.dashboard.sign.tabs.json}
              </button>
            </div>

            {/* Certificate Selector */}
            <div className={uiStyles.input.wrapper}>
              <label className={uiStyles.input.label}>
                {t.dashboard.sign.form.certLabel}
              </label>
              <select
                value={selectedCertId}
                onChange={(e) => setSelectedCertId(e.target.value)}
                disabled={submitting}
                className={uiStyles.input.fieldBase}
              >
                <option value="" className="bg-[#0B0C10]">
                  {t.dashboard.sign.form.autoSelect}
                </option>
                {certs.map((c) => (
                  <option key={c.id} value={c.id} className="bg-[#0B0C10]">
                    {c.subjectName}
                  </option>
                ))}
              </select>
            </div>

            {errorMsg && (
              <div className={uiStyles.alert.error} role="alert">
                <WarningCircle size={18} weight="fill" className="shrink-0 mt-0.5" />
                <span>{errorMsg}</span>
              </div>
            )}

            {mode === "pdf" ? (
              <form onSubmit={handleSignPdf} className="space-y-4">
                <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
                  <FormField
                    label={t.dashboard.sign.form.reasonLabel}
                    placeholder={t.dashboard.sign.form.reasonPlaceholder}
                    value={reason}
                    onChange={(e) => setReason(e.target.value)}
                    disabled={submitting}
                  />
                  <FormField
                    label={t.dashboard.sign.form.locationLabel}
                    placeholder={t.dashboard.sign.form.locationPlaceholder}
                    value={location}
                    onChange={(e) => setLocation(e.target.value)}
                    disabled={submitting}
                  />
                </div>

                <label
                  htmlFor={pdfInputId}
                  onDragOver={(e) => {
                    e.preventDefault();
                    if (!submitting) setIsDragging(true);
                  }}
                  onDragLeave={() => setIsDragging(false)}
                  onDrop={(e) => {
                    e.preventDefault();
                    setIsDragging(false);
                    void handleSelectPdf(e.dataTransfer.files?.[0]);
                  }}
                  className={clsx(
                    uiStyles.dropzone.base,
                    isDragging && uiStyles.dropzone.dragging,
                    submitting && "pointer-events-none opacity-60"
                  )}
                >
                  <input
                    id={pdfInputId}
                    type="file"
                    accept="application/pdf,.pdf"
                    disabled={submitting}
                    className="sr-only"
                    onChange={(e) => {
                      void handleSelectPdf(e.target.files?.[0]);
                      e.target.value = "";
                    }}
                  />
                  <div className={uiStyles.dropzone.iconWrap}>
                    {pdfFile ? (
                      <FilePdf size={24} weight="duotone" />
                    ) : (
                      <UploadSimple size={24} weight="light" />
                    )}
                  </div>
                  <div className="space-y-1">
                    <p className="text-sm font-medium text-white">
                      {pdfFile
                        ? pdfFile.name
                        : t.dashboard.sign.form.pdfDropTitle}
                    </p>
                    <p className="text-xs text-zinc-400">
                      {t.dashboard.sign.form.pdfDropHint}
                    </p>
                  </div>
                </label>

                <IslandButton
                  type="submit"
                  disabled={!pdfFile}
                  label={t.dashboard.sign.form.submitPdf}
                  loadingLabel={t.dashboard.sign.form.submitting}
                  isLoading={submitting}
                />
              </form>
            ) : (
              <form onSubmit={handleSignJson} className="space-y-4">
                <div className={uiStyles.input.wrapper}>
                  <div className="flex flex-wrap items-center justify-between gap-2">
                    <label className={uiStyles.input.label}>
                      {t.dashboard.sign.form.jsonLabel}
                    </label>
                    <button
                      type="button"
                      onClick={() =>
                        setJsonText(
                          JSON.stringify(
                            {
                              documentType: "Certificate",
                              recipient: "Nguyen Van A",
                              issuedYear: 2026,
                            },
                            null,
                            2
                          )
                        )
                      }
                      className={uiStyles.button.ghostPill}
                    >
                      {t.dashboard.sign.form.loadSampleJson}
                    </button>
                  </div>
                  <textarea
                    rows={6}
                    value={jsonText}
                    onChange={(e) => setJsonText(e.target.value)}
                    placeholder={t.dashboard.sign.form.jsonPlaceholder}
                    disabled={submitting}
                    className={`${uiStyles.input.fieldBase} font-mono text-xs leading-relaxed`}
                  />
                </div>

                <IslandButton
                  type="submit"
                  disabled={!jsonText.trim()}
                  label={t.dashboard.sign.form.submitJson}
                  loadingLabel={t.dashboard.sign.form.submitting}
                  isLoading={submitting}
                />
              </form>
            )}

            {pdfSuccess && (
              <div className={uiStyles.resultCard.valid} role="status">
                <div className="flex items-start gap-3">
                  <CheckCircle size={22} weight="fill" className="text-emerald-400 shrink-0 mt-0.5" />
                  <div className="space-y-1">
                    <p className="text-sm font-semibold text-white">
                      {t.dashboard.sign.result.pdfSuccess}
                    </p>
                    <p className="text-xs text-zinc-300">
                      {t.dashboard.sign.result.fileName}: {pdfSuccess.fileName}
                    </p>
                  </div>
                </div>
                {pdfSuccess.documentCode && (
                  <div className="flex flex-wrap items-center justify-between gap-2 pt-2 border-t border-white/[0.08]">
                    <span className={uiStyles.typography.monoCode}>
                      {pdfSuccess.documentCode}
                    </span>
                    <Link
                      href={`/verify/${encodeURIComponent(pdfSuccess.documentCode)}`}
                      className={uiStyles.button.ghostPill}
                    >
                      <span>{t.dashboard.sign.result.verifyLink}</span>
                      <ArrowUpRight size={14} weight="light" />
                    </Link>
                  </div>
                )}
              </div>
            )}

            {jsonSuccess && (
              <div className={uiStyles.resultCard.valid} role="status">
                <div className="flex items-start gap-3">
                  <CheckCircle size={22} weight="fill" className="text-emerald-400 shrink-0 mt-0.5" />
                  <p className="text-sm font-semibold text-white">
                    {t.dashboard.sign.result.jsonSuccess}
                  </p>
                </div>
                <p className="font-mono text-xs text-zinc-300 break-all bg-black/30 p-3 rounded-xl">
                  {jsonSuccess.jws}
                </p>
                <div className="flex flex-wrap items-center justify-between gap-2">
                  <CopyButton
                    value={jsonSuccess.jws}
                    label={t.dashboard.sign.result.copySignedData}
                    copiedLabel={t.dashboard.sign.result.copied}
                  />
                  <Link
                    href={`/verify/${encodeURIComponent(jsonSuccess.documentCode)}`}
                    className={uiStyles.button.ghostPill}
                  >
                    <span>{t.dashboard.sign.result.verifyLink}</span>
                    <ArrowUpRight size={14} weight="light" />
                  </Link>
                </div>
              </div>
            )}
          </div>
        </DoubleBezelCard>
      )}
    </div>
  );
}
