"use client";

import React, { useEffect, useState } from "react";
import Link from "next/link";
import {
  ArrowLeft,
  CircleNotch,
  ClockCounterClockwise,
  WarningCircle,
} from "@phosphor-icons/react";
import { DoubleBezelCard } from "@/components/ui/double-bezel-card";
import { EmptyState } from "@/components/ui/empty-state";
import type {
  SignedDocumentDto,
  VerificationLogDto,
} from "@/features/documents/types/document.types";
import { mapErrorMessage } from "@/i18n/map-error-message";
import { useTranslation } from "@/i18n/language-context";
import { documentService } from "@/services/document.service";
import { uiStyles } from "@/styles/ui.styles";

export function DocumentVerificationsView({
  documentId,
}: {
  documentId: string;
}) {
  const { t, locale } = useTranslation();
  const [doc, setDoc] = useState<SignedDocumentDto | null>(null);
  const [logs, setLogs] = useState<VerificationLogDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<unknown>(null);

  useEffect(() => {
    let cancelled = false;
    (async () => {
      try {
        const [docRes, logsRes] = await Promise.all([
          documentService.getById(documentId),
          documentService.getVerificationHistory(documentId, 0, 50),
        ]);
        if (!cancelled) {
          setDoc(docRes);
          setLogs(logsRes);
        }
      } catch (err) {
        if (!cancelled) {
          setError(err);
        }
      } finally {
        if (!cancelled) {
          setLoading(false);
        }
      }
    })();

    return () => {
      cancelled = true;
    };
  }, [documentId]);

  const formatDateTime = (iso: string) =>
    new Date(iso).toLocaleString(locale === "vi" ? "vi-VN" : "en-US");

  return (
    <div className="space-y-6">
      <Link href="/documents" className={uiStyles.button.ghostPill}>
        <ArrowLeft size={15} weight="light" />
        <span>{t.dashboard.documents.history.back}</span>
      </Link>

      {loading && (
        <div className="flex items-center gap-3 py-12 text-sm text-zinc-400">
          <CircleNotch size={20} weight="light" className="animate-spin text-emerald-400" />
          <span>{t.common.loading}</span>
        </div>
      )}

      {error !== null && (
        <div className={uiStyles.alert.error} role="alert">
          <WarningCircle size={18} weight="fill" className="shrink-0 mt-0.5" />
          <span>{mapErrorMessage(error, t.errors)}</span>
        </div>
      )}

      {!loading && error === null && doc && (
        <DoubleBezelCard>
          <div className="space-y-5">
            <div className="space-y-1.5">
              <h1 className={uiStyles.typography.sectionTitle}>
                {t.dashboard.documents.history.title}
              </h1>
              <p className={uiStyles.typography.bodyMuted}>
                {doc.documentName} •{" "}
                <span className={uiStyles.typography.monoCode}>
                  {doc.documentCode}
                </span>
              </p>
            </div>

            {logs.length === 0 ? (
              <EmptyState
                icon={<ClockCounterClockwise size={24} weight="light" />}
                title={t.dashboard.documents.history.emptyTitle}
                description={t.dashboard.documents.history.emptyDesc}
              />
            ) : (
              <div className="space-y-2.5">
                {logs.map((log) => {
                  const statusBadge =
                    log.validationStatus === 1
                      ? uiStyles.badge.emerald
                      : log.validationStatus === 2 || log.validationStatus === 3
                      ? uiStyles.badge.danger
                      : uiStyles.badge.amber;

                  return (
                    <div
                      key={log.id}
                      className="flex flex-col sm:flex-row sm:items-center justify-between gap-2.5 rounded-2xl bg-white/[0.02] p-4 ring-1 ring-white/[0.06]"
                    >
                      <div className="space-y-1">
                        <div className="flex flex-wrap items-center gap-2">
                          <span className={statusBadge}>
                            {t.dashboard.documents.history.statuses[log.validationStatus]}
                          </span>
                          <span className={uiStyles.badge.zinc}>
                            {t.dashboard.documents.history.methods[log.verificationMethod]}
                          </span>
                        </div>
                        <p className="text-xs text-zinc-400">
                          {t.dashboard.documents.history.fields.time}:{" "}
                          {formatDateTime(log.verifiedAt)}
                        </p>
                      </div>

                      <p className="font-mono text-xs text-zinc-400">
                        {t.dashboard.documents.history.fields.ip}: {log.verifierIp}
                      </p>
                    </div>
                  );
                })}
              </div>
            )}
          </div>
        </DoubleBezelCard>
      )}
    </div>
  );
}
