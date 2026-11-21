"use client";

import React from "react";
import Link from "next/link";
import { ChartBar, ShieldCheck } from "@phosphor-icons/react";
import { LanguageSwitcher } from "@/components/ui/language-switcher";
import { useAuth } from "@/features/auth/context/auth-context";
import { useTranslation } from "@/i18n/language-context";
import { uiStyles } from "@/styles/ui.styles";

export function PublicHeader() {
  const { t } = useTranslation();
  const { isAuthenticated, isBootstrapping } = useAuth();

  return (
    <header className={uiStyles.layout.publicHeader}>
      <div className={uiStyles.layout.publicHeaderInner}>
        <Link href="/verify" className="flex items-center gap-2.5 min-h-[44px]">
          <div className="flex h-9 w-9 items-center justify-center rounded-xl bg-emerald-500/10 ring-1 ring-emerald-500/25">
            <ShieldCheck size={20} weight="duotone" className="text-emerald-400" />
          </div>
          <span className="text-base font-semibold tracking-tight text-white">
            {t.common.brandName}
          </span>
        </Link>

        <div className="flex items-center gap-2">
          <LanguageSwitcher />
          {!isBootstrapping &&
            (isAuthenticated ? (
              <Link href="/overview" className={uiStyles.button.verifyCtaPill}>
                <ChartBar size={16} weight="duotone" />
                <span>{t.dashboard.nav.overview}</span>
              </Link>
            ) : (
              <Link href="/login" className={uiStyles.button.ghostPill}>
                <span>{t.verify.header.signInLink}</span>
              </Link>
            ))}
        </div>
      </div>
    </header>
  );
}
