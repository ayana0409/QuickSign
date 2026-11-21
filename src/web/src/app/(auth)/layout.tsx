"use client";

import React from "react";
import { ShieldCheck } from "@phosphor-icons/react";
import { LanguageSwitcher } from "@/components/ui/language-switcher";
import { useTranslation } from "@/i18n/language-context";
import { uiStyles } from "@/styles/ui.styles";

export default function AuthLayout({ children }: { children: React.ReactNode }) {
  const { t } = useTranslation();

  return (
    <div className={uiStyles.layout.authSplitContainer}>
      {/* Left Brand & Cryptographic Telemetry Pane */}
      <aside className={uiStyles.layout.authBrandPane}>
        <div
          className="pointer-events-none absolute -top-32 -left-32 h-96 w-96 rounded-full bg-emerald-500/10 blur-3xl"
          aria-hidden="true"
        />

        <div className="relative flex items-center gap-3">
          <div className="flex h-10 w-10 items-center justify-center rounded-2xl bg-emerald-500/10 ring-1 ring-emerald-500/25">
            <ShieldCheck size={22} weight="duotone" className="text-emerald-400" />
          </div>
          <span className="text-lg font-semibold tracking-tight text-white">
            {t.common.brandName}
          </span>
        </div>

        <div className="relative max-w-xl space-y-6 my-auto py-12">
          <span className={uiStyles.typography.eyebrowBadge}>
            {t.auth.brandPanel.eyebrow}
          </span>
          <h2 className={uiStyles.typography.heroHeading}>
            {t.auth.brandPanel.headline}
          </h2>
          <p className={uiStyles.typography.bodyMuted}>
            {t.auth.brandPanel.description}
          </p>

          <div className="grid grid-cols-2 gap-3 pt-4">
            {t.auth.brandPanel.metrics.map((metric) => (
              <div key={metric.label} className={uiStyles.card.metricTile}>
                <p className="text-[10px] font-medium uppercase tracking-[0.18em] text-zinc-500">
                  {metric.label}
                </p>
                <p className="mt-1.5 font-mono text-xs text-zinc-200">{metric.value}</p>
              </div>
            ))}
          </div>
        </div>

        <p className="relative text-xs text-zinc-500">{t.common.brandTagline}</p>
      </aside>

      {/* Right Interactive Auth Pane */}
      <main className={uiStyles.layout.authFormPane}>
        <div className="flex items-center justify-between lg:justify-end">
          <div className="flex items-center gap-2.5 lg:hidden">
            <ShieldCheck size={22} weight="duotone" className="text-emerald-400" />
            <span className="text-base font-semibold tracking-tight text-white">
              {t.common.brandName}
            </span>
          </div>
          <LanguageSwitcher />
        </div>

        <div className={uiStyles.layout.authFormCenter}>{children}</div>

        <div className="h-6" aria-hidden="true" />
      </main>
    </div>
  );
}
