"use client";

import React, { useCallback, useEffect, useState } from "react";
import {
  CaretDown,
  Certificate,
  CircleNotch,
  Plus,
  Prohibit,
  WarningCircle,
} from "@phosphor-icons/react";
import { CopyButton } from "@/components/ui/copy-button";
import { DoubleBezelCard } from "@/components/ui/double-bezel-card";
import { EmptyState } from "@/components/ui/empty-state";
import { FormField } from "@/components/ui/form-field";
import { IslandButton } from "@/components/ui/island-button";
import { ModalDialog } from "@/components/ui/modal-dialog";
import type {
  CertificateResponseDto,
  KeyAlgorithmType,
} from "@/features/certificates/types/certificate.types";
import { mapErrorMessage } from "@/i18n/map-error-message";
import { useTranslation } from "@/i18n/language-context";
import { certificateService } from "@/services/certificate.service";
import { uiStyles } from "@/styles/ui.styles";

export function CertificatesManager() {
  const { t, locale } = useTranslation();
  const [certs, setCerts] = useState<CertificateResponseDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [loadError, setLoadError] = useState<unknown>(null);

  // Issue modal state
  const [issueOpen, setIssueOpen] = useState(false);
  const [subjectName, setSubjectName] = useState("");
  const [algorithm, setAlgorithm] = useState<KeyAlgorithmType>(1);
  const [validityDays, setValidityDays] = useState("365");
  const [issueError, setIssueError] = useState<string | null>(null);
  const [issuing, setIssuing] = useState(false);

  // Revoke modal state
  const [revokeTarget, setRevokeTarget] = useState<CertificateResponseDto | null>(null);
  const [invalidatedFrom, setInvalidatedFrom] = useState("");
  const [revokeReason, setRevokeReason] = useState("");
  const [revokeError, setRevokeError] = useState<string | null>(null);
  const [revoking, setRevoking] = useState(false);

  const loadCertificates = useCallback(async () => {
    setLoading(true);
    setLoadError(null);
    try {
      const list = await certificateService.getMyCertificates();
      setCerts(list);
    } catch (err) {
      setLoadError(err);
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    void loadCertificates();
  }, [loadCertificates]);

  const activeCount = certs.filter((c) => c.status === 1).length;
  const limitReached = activeCount >= 5;

  const handleIssue = async (e: React.FormEvent) => {
    e.preventDefault();
    setIssueError(null);

    if (!subjectName.trim()) {
      setIssueError(t.dashboard.certificates.issueModal.subjectRequired);
      return;
    }

    const days = Number(validityDays);
    if (!Number.isInteger(days) || days < 1 || days > 3650) {
      setIssueError(t.dashboard.certificates.issueModal.validityInvalid);
      return;
    }

    setIssuing(true);
    try {
      await certificateService.issueCertificate({
        subjectName: subjectName.trim(),
        algorithm,
        validityDays: days,
      });
      setIssueOpen(false);
      setSubjectName("");
      setValidityDays("365");
      await loadCertificates();
    } catch (err) {
      setIssueError(mapErrorMessage(err, t.errors));
    } finally {
      setIssuing(false);
    }
  };

  const handleRevoke = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!revokeTarget) return;
    setRevokeError(null);
    setRevoking(true);

    try {
      await certificateService.revokeCertificate(revokeTarget.id, {
        invalidatedFrom: invalidatedFrom
          ? new Date(invalidatedFrom).toISOString()
          : null,
        reason: revokeReason.trim() || null,
      });
      setRevokeTarget(null);
      setInvalidatedFrom("");
      setRevokeReason("");
      await loadCertificates();
    } catch (err) {
      setRevokeError(mapErrorMessage(err, t.errors));
    } finally {
      setRevoking(false);
    }
  };

  const formatDate = (iso: string) =>
    new Date(iso).toLocaleDateString(locale === "vi" ? "vi-VN" : "en-US");

  return (
    <div className="space-y-6">
      <div className="flex flex-col sm:flex-row sm:items-end justify-between gap-4">
        <div className="space-y-1.5">
          <span className={uiStyles.typography.eyebrowBadge}>
            {t.dashboard.certificates.eyebrow}
          </span>
          <h1 className={uiStyles.typography.sectionTitle}>
            {t.dashboard.certificates.title}
          </h1>
          <p className={uiStyles.typography.bodyMuted}>
            {t.dashboard.certificates.subtitle}
          </p>
        </div>

        <div className="flex flex-wrap items-center gap-3">
          <span className={uiStyles.badge.emerald}>
            {t.dashboard.certificates.quotaLabel.replace(
              "{active}",
              String(activeCount)
            )}
          </span>
          <button
            type="button"
            disabled={limitReached}
            onClick={() => {
              setIssueError(null);
              setIssueOpen(true);
            }}
            className="inline-flex min-h-[44px] items-center gap-2 rounded-full bg-white px-5 py-2 text-xs font-semibold text-zinc-950 transition-colors hover:bg-zinc-100 disabled:opacity-45 disabled:pointer-events-none cursor-pointer"
          >
            <Plus size={15} weight="bold" />
            <span>{t.dashboard.certificates.issueButton}</span>
          </button>
        </div>
      </div>

      {limitReached && (
        <div className={uiStyles.alert.warning} role="status">
          <WarningCircle size={18} weight="fill" className="shrink-0 mt-0.5" />
          <span>{t.dashboard.certificates.limitReached}</span>
        </div>
      )}

      {loading && (
        <div className="flex items-center gap-3 py-12 text-sm text-zinc-400">
          <CircleNotch size={20} weight="light" className="animate-spin text-emerald-400" />
          <span>{t.common.loading}</span>
        </div>
      )}

      {loadError !== null && (
        <div className={uiStyles.alert.error} role="alert">
          <WarningCircle size={18} weight="fill" className="shrink-0 mt-0.5" />
          <span>{mapErrorMessage(loadError, t.errors)}</span>
        </div>
      )}

      {!loading && loadError === null && certs.length === 0 && (
        <EmptyState
          icon={<Certificate size={24} weight="light" />}
          title={t.dashboard.certificates.emptyTitle}
          description={t.dashboard.certificates.emptyDesc}
          action={
            <button
              type="button"
              onClick={() => setIssueOpen(true)}
              className={uiStyles.button.ghostPill}
            >
              <Plus size={15} weight="light" />
              <span>{t.dashboard.certificates.issueButton}</span>
            </button>
          }
        />
      )}

      {!loading && certs.length > 0 && (
        <div className="space-y-4">
          {certs.map((cert) => {
            const badgeClass =
              cert.status === 1
                ? uiStyles.badge.emerald
                : cert.status === 3
                ? uiStyles.badge.danger
                : uiStyles.badge.amber;

            return (
              <DoubleBezelCard key={cert.id}>
                <div className="space-y-4">
                  <div className="flex flex-col sm:flex-row sm:items-start justify-between gap-3">
                    <div className="space-y-1.5 min-w-0">
                      <div className="flex flex-wrap items-center gap-2">
                        <span className={badgeClass}>
                          {t.dashboard.certificates.statuses[cert.status]}
                        </span>
                        <span className={uiStyles.badge.zinc}>
                          {t.dashboard.certificates.algorithms[cert.algorithm]}
                        </span>
                      </div>
                      <h2 className="text-base font-semibold text-white break-words">
                        {cert.subjectName}
                      </h2>
                    </div>

                    {cert.status === 1 && (
                      <button
                        type="button"
                        onClick={() => {
                          setRevokeError(null);
                          setRevokeTarget(cert);
                        }}
                        className={uiStyles.button.dangerPill}
                      >
                        <Prohibit size={15} weight="light" />
                        <span>{t.dashboard.certificates.revokeModal.title}</span>
                      </button>
                    )}
                  </div>

                  <div className="grid grid-cols-2 sm:grid-cols-3 gap-2.5">
                    <div className={uiStyles.card.metricTile}>
                      <p className="text-[11px] text-zinc-500">
                        {t.dashboard.certificates.fields.createdAt}
                      </p>
                      <p className="mt-1 text-xs sm:text-sm text-zinc-200">
                        {formatDate(cert.createdAt)}
                      </p>
                    </div>
                    <div className={uiStyles.card.metricTile}>
                      <p className="text-[11px] text-zinc-500">
                        {t.dashboard.certificates.fields.expiresAt}
                      </p>
                      <p className="mt-1 text-xs sm:text-sm text-zinc-200">
                        {formatDate(cert.expiresAt)}
                      </p>
                    </div>
                    {cert.revokedAt && (
                      <div className={uiStyles.card.metricTile}>
                        <p className="text-[11px] text-zinc-500">
                          {t.dashboard.certificates.fields.revokedAt}
                        </p>
                        <p className="mt-1 text-xs sm:text-sm text-rose-300">
                          {formatDate(cert.revokedAt)}
                        </p>
                      </div>
                    )}
                  </div>

                  {cert.revokedReason && (
                    <p className="text-xs text-rose-300">
                      {t.dashboard.certificates.fields.revokedReason}:{" "}
                      {cert.revokedReason}
                    </p>
                  )}

                  <details className={uiStyles.detailsDrawer.root}>
                    <summary className={uiStyles.detailsDrawer.summary}>
                      <span>{t.dashboard.certificates.fields.detailsToggle}</span>
                      <CaretDown size={14} weight="light" />
                    </summary>
                    <div className={uiStyles.detailsDrawer.body}>
                      <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-2">
                        <div className="min-w-0 space-y-0.5">
                          <p className="text-[11px] uppercase tracking-wider text-zinc-500">
                            {t.dashboard.certificates.fields.serial}
                          </p>
                          <p className="font-mono text-xs text-zinc-200 break-all">
                            {cert.serialNumber}
                          </p>
                        </div>
                        <div className="shrink-0">
                          <CopyButton
                            value={cert.serialNumber}
                            label={t.dashboard.certificates.fields.copy}
                            copiedLabel={t.dashboard.certificates.fields.copied}
                          />
                        </div>
                      </div>

                      <div className="flex flex-col sm:flex-row sm:items-start sm:justify-between gap-2 pt-2 border-t border-white/[0.05]">
                        <div className="min-w-0 space-y-0.5 flex-1">
                          <p className="text-[11px] uppercase tracking-wider text-zinc-500">
                            {t.dashboard.certificates.fields.publicKey}
                          </p>
                          <pre className="font-mono text-[11px] text-zinc-400 whitespace-pre-wrap break-all max-h-28 overflow-y-auto">
                            {cert.publicKeyPem}
                          </pre>
                        </div>
                        <div className="shrink-0">
                          <CopyButton
                            value={cert.publicKeyPem}
                            label={t.dashboard.certificates.fields.copy}
                            copiedLabel={t.dashboard.certificates.fields.copied}
                          />
                        </div>
                      </div>
                    </div>
                  </details>
                </div>
              </DoubleBezelCard>
            );
          })}
        </div>
      )}

      {/* Issue Certificate Modal */}
      <ModalDialog
        open={issueOpen}
        onClose={() => setIssueOpen(false)}
        title={t.dashboard.certificates.issueModal.title}
      >
        <form onSubmit={handleIssue} className="space-y-4">
          {issueError && (
            <div className={uiStyles.alert.error} role="alert">
              <WarningCircle size={18} weight="fill" className="shrink-0 mt-0.5" />
              <span>{issueError}</span>
            </div>
          )}

          <FormField
            label={t.dashboard.certificates.issueModal.subjectLabel}
            placeholder={t.dashboard.certificates.issueModal.subjectPlaceholder}
            value={subjectName}
            onChange={(e) => setSubjectName(e.target.value)}
            disabled={issuing}
          />

          <div className={uiStyles.input.wrapper}>
            <label className={uiStyles.input.label}>
              {t.dashboard.certificates.issueModal.algorithmLabel}
            </label>
            <select
              value={algorithm}
              onChange={(e) => setAlgorithm(Number(e.target.value) as KeyAlgorithmType)}
              disabled={issuing}
              className={uiStyles.input.fieldBase}
            >
              <option value={1} className="bg-[#0B0C10]">
                {t.dashboard.certificates.algorithms[1]}
              </option>
              <option value={2} className="bg-[#0B0C10]">
                {t.dashboard.certificates.algorithms[2]}
              </option>
            </select>
          </div>

          <FormField
            label={t.dashboard.certificates.issueModal.validityLabel}
            type="number"
            min={1}
            max={3650}
            value={validityDays}
            onChange={(e) => setValidityDays(e.target.value)}
            disabled={issuing}
          />

          <div className="flex flex-col-reverse sm:flex-row justify-end gap-2.5 pt-2">
            <button
              type="button"
              onClick={() => setIssueOpen(false)}
              className={uiStyles.button.ghostPill}
            >
              {t.dashboard.certificates.issueModal.cancel}
            </button>
            <IslandButton
              type="submit"
              label={t.dashboard.certificates.issueModal.submit}
              loadingLabel={t.dashboard.certificates.issueModal.submitting}
              isLoading={issuing}
              className="sm:w-auto"
            />
          </div>
        </form>
      </ModalDialog>

      {/* Revoke Certificate Modal */}
      <ModalDialog
        open={Boolean(revokeTarget)}
        onClose={() => setRevokeTarget(null)}
        title={t.dashboard.certificates.revokeModal.title}
      >
        <form onSubmit={handleRevoke} className="space-y-4">
          <div className={uiStyles.alert.warning} role="status">
            <WarningCircle size={18} weight="fill" className="shrink-0 mt-0.5" />
            <span>{t.dashboard.certificates.revokeModal.warning}</span>
          </div>

          {revokeError && (
            <div className={uiStyles.alert.error} role="alert">
              <WarningCircle size={18} weight="fill" className="shrink-0 mt-0.5" />
              <span>{revokeError}</span>
            </div>
          )}

          <FormField
            label={t.dashboard.certificates.revokeModal.dateLabel}
            type="datetime-local"
            value={invalidatedFrom}
            onChange={(e) => setInvalidatedFrom(e.target.value)}
            disabled={revoking}
          />

          <FormField
            label={t.dashboard.certificates.revokeModal.reasonLabel}
            placeholder={t.dashboard.certificates.revokeModal.reasonPlaceholder}
            value={revokeReason}
            onChange={(e) => setRevokeReason(e.target.value)}
            disabled={revoking}
          />

          <div className="flex flex-col-reverse sm:flex-row justify-end gap-2.5 pt-2">
            <button
              type="button"
              onClick={() => setRevokeTarget(null)}
              className={uiStyles.button.ghostPill}
            >
              {t.dashboard.certificates.revokeModal.cancel}
            </button>
            <button
              type="submit"
              disabled={revoking}
              className={uiStyles.button.dangerPill}
            >
              <span>
                {revoking
                  ? t.dashboard.certificates.revokeModal.submitting
                  : t.dashboard.certificates.revokeModal.submit}
              </span>
            </button>
          </div>
        </form>
      </ModalDialog>
    </div>
  );
}
