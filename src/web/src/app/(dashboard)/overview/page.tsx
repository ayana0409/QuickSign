"use client";

import React, { useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import {
  ArrowsClockwise,
  CheckCircle,
  CircleNotch,
  ShieldCheck,
  SignOut,
} from "@phosphor-icons/react";
import { DoubleBezelCard } from "@/components/ui/double-bezel-card";
import { LanguageSwitcher } from "@/components/ui/language-switcher";
import { useAuth } from "@/features/auth/context/auth-context";
import { useTranslation } from "@/i18n/language-context";
import { uiStyles } from "@/styles/ui.styles";

export default function OverviewPage() {
  const router = useRouter();
  const { user, isAuthenticated, isBootstrapping, refreshSession, logout } = useAuth();
  const { t } = useTranslation();

  const [isRefreshing, setIsRefreshing] = useState(false);
  const [refreshMessage, setRefreshMessage] = useState<string | null>(null);

  useEffect(() => {
    if (!isBootstrapping && !isAuthenticated) {
      router.replace("/login");
    }
  }, [isBootstrapping, isAuthenticated, router]);

  if (isBootstrapping || !user) {
    return (
      <main className="min-h-[100dvh] flex flex-col items-center justify-center gap-3 px-4">
        <CircleNotch size={28} weight="light" className="animate-spin text-emerald-400" />
        <p className="text-sm text-zinc-400">{t.common.checkingSession}</p>
      </main>
    );
  }

  const handleTestRefresh = async () => {
    setIsRefreshing(true);
    setRefreshMessage(null);
    const ok = await refreshSession();
    setIsRefreshing(false);
    if (ok) {
      setRefreshMessage(t.auth.overviewShell.refreshSuccess);
    } else {
      router.replace("/login");
    }
  };

  const handleLogout = async () => {
    await logout();
    router.replace("/login");
  };

  const expiryDate = user.expiresAt
    ? new Date(user.expiresAt * 1000).toLocaleString()
    : "-";

  return (
    <main className={uiStyles.layout.overviewContainer}>
      <header className="flex flex-wrap items-center justify-between gap-4">
        <div className="flex items-center gap-3">
          <div className="flex h-10 w-10 items-center justify-center rounded-2xl bg-emerald-500/10 ring-1 ring-emerald-500/25">
            <ShieldCheck size={22} weight="duotone" className="text-emerald-400" />
          </div>
          <span className="text-lg font-semibold tracking-tight text-white">
            {t.common.brandName}
          </span>
        </div>

        <div className="flex items-center gap-2.5">
          <LanguageSwitcher />
          <button
            type="button"
            onClick={handleLogout}
            className={uiStyles.button.dangerPill}
          >
            <SignOut size={15} weight="light" />
            <span>{t.common.logout}</span>
          </button>
        </div>
      </header>

      <DoubleBezelCard>
        <div className="space-y-6">
          <div className="space-y-2">
            <span className={uiStyles.typography.eyebrowBadge}>
              {t.auth.overviewShell.badge}
            </span>
            <h1 className={uiStyles.typography.sectionTitle}>
              {t.auth.overviewShell.title}
            </h1>
            <p className={uiStyles.typography.bodyMuted}>
              {t.auth.overviewShell.subtitle}
            </p>
          </div>

          {refreshMessage && (
            <div className={uiStyles.alert.success} role="status">
              <CheckCircle size={18} weight="fill" className="shrink-0 mt-0.5" />
              <span>{refreshMessage}</span>
            </div>
          )}

          <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
            <div className={uiStyles.card.metricTile}>
              <p className="text-[11px] uppercase tracking-wider text-zinc-500">
                {t.auth.overviewShell.userIdLabel}
              </p>
              <p className="mt-1 font-mono text-xs text-zinc-200 break-all">
                {user.userId}
              </p>
            </div>
            <div className={uiStyles.card.metricTile}>
              <p className="text-[11px] uppercase tracking-wider text-zinc-500">
                {t.auth.overviewShell.userNameLabel}
              </p>
              <p className="mt-1 text-sm font-medium text-white">{user.userName}</p>
            </div>
            <div className={uiStyles.card.metricTile}>
              <p className="text-[11px] uppercase tracking-wider text-zinc-500">
                {t.auth.overviewShell.emailLabel}
              </p>
              <p className="mt-1 text-sm text-zinc-300">{user.email ?? "-"}</p>
            </div>
            <div className={uiStyles.card.metricTile}>
              <p className="text-[11px] uppercase tracking-wider text-zinc-500">
                {t.auth.overviewShell.rolesLabel}
              </p>
              <div className="mt-1.5 flex flex-wrap gap-1.5">
                {(user.roles.length > 0 ? user.roles : [t.common.roleUser]).map((r) => (
                  <span key={r} className={uiStyles.badge.emerald}>
                    {r}
                  </span>
                ))}
              </div>
            </div>
            <div className={`${uiStyles.card.metricTile} sm:col-span-2`}>
              <p className="text-[11px] uppercase tracking-wider text-zinc-500">
                {t.auth.overviewShell.expiresLabel}
              </p>
              <p className="mt-1 font-mono text-xs text-zinc-300">{expiryDate}</p>
            </div>
          </div>

          <div className="flex flex-wrap items-center justify-between gap-4 pt-2 border-t border-white/[0.06]">
            <p className="text-xs text-zinc-500 max-w-md">
              {t.auth.overviewShell.nextStepNote}
            </p>
            <button
              type="button"
              onClick={handleTestRefresh}
              disabled={isRefreshing}
              className={uiStyles.button.ghostPill}
            >
              <ArrowsClockwise
                size={15}
                weight="light"
                className={isRefreshing ? "animate-spin" : ""}
              />
              <span>
                {isRefreshing
                  ? t.auth.overviewShell.refreshingButton
                  : t.auth.overviewShell.refreshButton}
              </span>
            </button>
          </div>
        </div>
      </DoubleBezelCard>
    </main>
  );
}
