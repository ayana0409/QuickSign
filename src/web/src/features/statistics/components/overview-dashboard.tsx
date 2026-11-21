"use client";

import React, { useEffect, useState } from "react";
import Link from "next/link";
import {
  ArrowRight,
  Certificate,
  CircleNotch,
  Files,
  PenNib,
  WarningCircle,
} from "@phosphor-icons/react";
import { DoubleBezelCard } from "@/components/ui/double-bezel-card";
import { EmptyState } from "@/components/ui/empty-state";
import type { SignedDocumentDto } from "@/features/documents/types/document.types";
import type { DashboardOverviewStatsDto } from "@/features/statistics/types/statistics.types";
import { mapErrorMessage } from "@/i18n/map-error-message";
import { useTranslation } from "@/i18n/language-context";
import { documentService } from "@/services/document.service";
import { statisticsService } from "@/services/statistics.service";
import { uiStyles } from "@/styles/ui.styles";

export function OverviewDashboard() {
  const { t, locale } = useTranslation();
  const [stats, setStats] = useState<DashboardOverviewStatsDto | null>(null);
  const [recentDocs, setRecentDocs] = useState<SignedDocumentDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<unknown>(null);

  useEffect(() => {
    let cancelled = false;
    (async () => {
      try {
        const [overviewRes, docsRes] = await Promise.all([
          statisticsService.getMyOverview(),
          documentService.getMyDocuments(0, 5),
        ]);
        if (!cancelled) {
          setStats(overviewRes);
          setRecentDocs(docsRes);
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
  }, []);

  if (loading) {
    return (
      <div className="flex items-center gap-3 py-12 text-sm text-zinc-400">
        <CircleNotch size={20} weight="light" className="animate-spin text-emerald-400" />
        <span>{t.common.loading}</span>
      </div>
    );
  }

  if (error) {
    return (
      <div className={uiStyles.alert.error} role="alert">
        <WarningCircle size={18} weight="fill" className="shrink-0 mt-0.5" />
        <span>{mapErrorMessage(error, t.errors)}</span>
      </div>
    );
  }

  const kpis = [
    {
      label: t.dashboard.overview.kpis.totalSigned,
      value: stats?.totalSignedDocuments ?? 0,
      tone: "text-white",
    },
    {
      label: t.dashboard.overview.kpis.totalVerifications,
      value: stats?.totalVerifications ?? 0,
      tone: "text-white",
    },
    {
      label: t.dashboard.overview.kpis.uniqueVerifications,
      value: stats?.uniqueVerifications ?? 0,
      tone: "text-emerald-400",
    },
    {
      label: t.dashboard.overview.kpis.tamperedAlerts,
      value: stats?.tamperedAlerts ?? 0,
      tone: (stats?.tamperedAlerts ?? 0) > 0 ? "text-rose-400" : "text-zinc-300",
    },
  ];

  const activeCerts = stats?.activeCertificates ?? 0;
  const quotaPct = Math.min(100, (activeCerts / 5) * 100);

  return (
    <div className="space-y-6">
      <div className="space-y-1.5">
        <span className={uiStyles.typography.eyebrowBadge}>
          {t.dashboard.overview.eyebrow}
        </span>
        <h1 className={uiStyles.typography.sectionTitle}>
          {t.dashboard.overview.title}
        </h1>
        <p className={uiStyles.typography.bodyMuted}>
          {t.dashboard.overview.subtitle}
        </p>
      </div>

      {/* Headline KPIs (F-Pattern Row 1) */}
      <div className="grid grid-cols-2 lg:grid-cols-4 gap-3 sm:gap-4">
        {kpis.map((kpi) => (
          <div key={kpi.label} className={uiStyles.card.metricTile}>
            <p className="text-xs text-zinc-400">{kpi.label}</p>
            <p className={`mt-2 text-2xl sm:text-3xl font-semibold tracking-tight ${kpi.tone}`}>
              {kpi.value}
            </p>
          </div>
        ))}
      </div>

      {/* Quota + Quick Actions */}
      <DoubleBezelCard>
        <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
          <div className="space-y-2 flex-1">
            <div className="flex items-center justify-between gap-2">
              <p className="text-sm font-medium text-white">
                {t.dashboard.overview.quota.title}
              </p>
              <span className="font-mono text-xs text-emerald-400">
                {t.dashboard.overview.quota.countLabel.replace(
                  "{active}",
                  String(activeCerts)
                )}
              </span>
            </div>
            <div className="h-2 w-full overflow-hidden rounded-full bg-white/[0.06]">
              <div
                className="h-full rounded-full bg-emerald-400 transition-all duration-500"
                style={{ width: `${quotaPct}%` }}
              />
            </div>
            <p className="text-xs text-zinc-400">
              {t.dashboard.overview.quota.activeKeys.replace(
                "{count}",
                String(stats?.activeApiKeys ?? 0)
              )}
            </p>
          </div>

          <div className="flex flex-wrap items-center gap-2.5 shrink-0">
            <Link href="/certificates" className={uiStyles.button.ghostPill}>
              <Certificate size={15} weight="light" />
              <span>{t.dashboard.overview.quota.manageCerts}</span>
            </Link>
            <Link
              href="/sign"
              className="inline-flex min-h-[44px] items-center gap-2 rounded-full bg-emerald-400 px-5 py-2 text-xs font-semibold text-zinc-950 transition-colors hover:bg-emerald-300"
            >
              <PenNib size={15} weight="duotone" />
              <span>{t.dashboard.overview.quota.signNow}</span>
            </Link>
          </div>
        </div>
      </DoubleBezelCard>

      {/* Recent Signed Documents */}
      <DoubleBezelCard>
        <div className="space-y-4">
          <div className="flex items-center justify-between gap-3">
            <h2 className="text-base font-semibold text-white">
              {t.dashboard.overview.recent.title}
            </h2>
            <Link href="/documents" className={uiStyles.button.ghostPill}>
              <span>{t.dashboard.overview.recent.viewAll}</span>
              <ArrowRight size={14} weight="light" />
            </Link>
          </div>

          {recentDocs.length === 0 ? (
            <EmptyState
              icon={<Files size={22} weight="light" />}
              title={t.dashboard.overview.recent.emptyTitle}
              description={t.dashboard.overview.recent.emptyDesc}
            />
          ) : (
            <div className="space-y-2.5">
              {recentDocs.map((doc) => (
                <Link
                  key={doc.id}
                  href={`/documents/${encodeURIComponent(doc.id)}/verifications`}
                  className="flex flex-col sm:flex-row sm:items-center justify-between gap-2 rounded-2xl bg-white/[0.02] p-4 ring-1 ring-white/[0.06] transition-colors hover:bg-white/[0.04]"
                >
                  <div className="min-w-0 space-y-1">
                    <p className="text-sm font-medium text-white truncate">
                      {doc.documentName}
                    </p>
                    <div className="flex flex-wrap items-center gap-2">
                      <span className={uiStyles.typography.monoCode}>
                        {doc.documentCode}
                      </span>
                      <span className="text-xs text-zinc-500">
                        {new Date(doc.createdAt).toLocaleDateString(
                          locale === "vi" ? "vi-VN" : "en-US"
                        )}
                      </span>
                    </div>
                  </div>
                  <div className="flex items-center gap-2 shrink-0">
                    <span className={uiStyles.badge.zinc}>
                      {t.dashboard.documents.types[doc.docType]}
                    </span>
                    {doc.tamperedAlertCount > 0 && (
                      <span className={uiStyles.badge.danger}>
                        {doc.tamperedAlertCount}
                      </span>
                    )}
                  </div>
                </Link>
              ))}
            </div>
          )}
        </div>
      </DoubleBezelCard>
    </div>
  );
}
