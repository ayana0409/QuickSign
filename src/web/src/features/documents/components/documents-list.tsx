"use client";

import React, { useCallback, useEffect, useState } from "react";
import Link from "next/link";
import {
  ArrowLeft,
  ArrowRight,
  CircleNotch,
  ClockCounterClockwise,
  Files,
  WarningCircle,
} from "@phosphor-icons/react";
import { DoubleBezelCard } from "@/components/ui/double-bezel-card";
import { EmptyState } from "@/components/ui/empty-state";
import type { SignedDocumentDto } from "@/features/documents/types/document.types";
import { mapErrorMessage } from "@/i18n/map-error-message";
import { useTranslation } from "@/i18n/language-context";
import { documentService } from "@/services/document.service";
import { uiStyles } from "@/styles/ui.styles";

const PAGE_SIZE = 20;

export function DocumentsList() {
  const { t, locale } = useTranslation();
  const [docs, setDocs] = useState<SignedDocumentDto[]>([]);
  const [skipCount, setSkipCount] = useState(0);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<unknown>(null);

  const loadPage = useCallback(async (skip: number) => {
    setLoading(true);
    setError(null);
    try {
      const list = await documentService.getMyDocuments(skip, PAGE_SIZE);
      setDocs(list);
    } catch (err) {
      setError(err);
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    void loadPage(skipCount);
  }, [skipCount, loadPage]);

  const formatDate = (iso: string) =>
    new Date(iso).toLocaleDateString(locale === "vi" ? "vi-VN" : "en-US");

  return (
    <div className="space-y-6">
      <div className="space-y-1.5">
        <span className={uiStyles.typography.eyebrowBadge}>
          {t.dashboard.documents.eyebrow}
        </span>
        <h1 className={uiStyles.typography.sectionTitle}>
          {t.dashboard.documents.title}
        </h1>
        <p className={uiStyles.typography.bodyMuted}>
          {t.dashboard.documents.subtitle}
        </p>
      </div>

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

      {!loading && error === null && docs.length === 0 && (
        <EmptyState
          icon={<Files size={24} weight="light" />}
          title={t.dashboard.documents.emptyTitle}
          description={t.dashboard.documents.emptyDesc}
        />
      )}

      {!loading && docs.length > 0 && (
        <>
          {/* Desktop Table */}
          <div className={uiStyles.table.wrapper}>
            <table className={uiStyles.table.table}>
              <thead className={uiStyles.table.thead}>
                <tr>
                  <th className={uiStyles.table.th}>{t.dashboard.documents.columns.document}</th>
                  <th className={uiStyles.table.th}>{t.dashboard.documents.columns.type}</th>
                  <th className={uiStyles.table.th}>{t.dashboard.documents.columns.verifications}</th>
                  <th className={uiStyles.table.th}>{t.dashboard.documents.columns.alerts}</th>
                  <th className={uiStyles.table.th}>{t.dashboard.documents.columns.createdAt}</th>
                  <th className={uiStyles.table.th}>{t.dashboard.documents.columns.actions}</th>
                </tr>
              </thead>
              <tbody>
                {docs.map((doc) => (
                  <tr key={doc.id} className={uiStyles.table.tr}>
                    <td className={uiStyles.table.td}>
                      <p className="font-medium text-white truncate max-w-xs">
                        {doc.documentName}
                      </p>
                      <span className={uiStyles.typography.monoCode}>
                        {doc.documentCode}
                      </span>
                    </td>
                    <td className={uiStyles.table.td}>
                      <span className={uiStyles.badge.zinc}>
                        {t.dashboard.documents.types[doc.docType]}
                      </span>
                    </td>
                    <td className={uiStyles.table.td}>
                      <span className="text-zinc-200">{doc.totalVerifyCount}</span>
                      <span className="text-xs text-zinc-500 ml-1">
                        ({doc.uniqueVerifyCount})
                      </span>
                    </td>
                    <td className={uiStyles.table.td}>
                      {doc.tamperedAlertCount > 0 ? (
                        <span className={uiStyles.badge.danger}>
                          {doc.tamperedAlertCount}
                        </span>
                      ) : (
                        <span className="text-zinc-500">0</span>
                      )}
                    </td>
                    <td className={uiStyles.table.td}>
                      <span className="text-xs text-zinc-400">
                        {formatDate(doc.createdAt)}
                      </span>
                    </td>
                    <td className={uiStyles.table.td}>
                      <Link
                        href={`/documents/${encodeURIComponent(doc.id)}/verifications`}
                        className={uiStyles.button.ghostPill}
                      >
                        <ClockCounterClockwise size={14} weight="light" />
                        <span>{t.dashboard.documents.viewHistory}</span>
                      </Link>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>

          {/* Mobile Summary Cards */}
          <div className="space-y-3 md:hidden">
            {docs.map((doc) => (
              <DoubleBezelCard key={doc.id}>
                <div className="space-y-3">
                  <div className="flex items-start justify-between gap-2">
                    <div className="min-w-0 space-y-1">
                      <p className="text-sm font-semibold text-white truncate">
                        {doc.documentName}
                      </p>
                      <span className={uiStyles.typography.monoCode}>
                        {doc.documentCode}
                      </span>
                    </div>
                    <span className={uiStyles.badge.zinc}>
                      {t.dashboard.documents.types[doc.docType]}
                    </span>
                  </div>

                  <div className="flex flex-wrap items-center justify-between gap-2 text-xs text-zinc-400">
                    <span>
                      {t.dashboard.documents.columns.verifications}: {doc.totalVerifyCount}
                    </span>
                    <span>{formatDate(doc.createdAt)}</span>
                  </div>

                  <Link
                    href={`/documents/${encodeURIComponent(doc.id)}/verifications`}
                    className={`${uiStyles.button.ghostPill} w-full`}
                  >
                    <ClockCounterClockwise size={15} weight="light" />
                    <span>{t.dashboard.documents.viewHistory}</span>
                  </Link>
                </div>
              </DoubleBezelCard>
            ))}
          </div>

          {/* Pagination */}
          <div className="flex items-center justify-between gap-3">
            <button
              type="button"
              disabled={skipCount === 0}
              onClick={() => setSkipCount((prev) => Math.max(0, prev - PAGE_SIZE))}
              className={`${uiStyles.button.ghostPill} disabled:opacity-40 disabled:pointer-events-none`}
            >
              <ArrowLeft size={14} weight="light" />
              <span>{t.dashboard.documents.prevPage}</span>
            </button>
            <button
              type="button"
              disabled={docs.length < PAGE_SIZE}
              onClick={() => setSkipCount((prev) => prev + PAGE_SIZE)}
              className={`${uiStyles.button.ghostPill} disabled:opacity-40 disabled:pointer-events-none`}
            >
              <span>{t.dashboard.documents.nextPage}</span>
              <ArrowRight size={14} weight="light" />
            </button>
          </div>
        </>
      )}
    </div>
  );
}
