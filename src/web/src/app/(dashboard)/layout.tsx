"use client";

import React, { useEffect } from "react";
import Link from "next/link";
import { usePathname, useRouter } from "next/navigation";
import {
  Certificate,
  ChartBar,
  CircleNotch,
  FileMagnifyingGlass,
  Files,
  Key,
  PenNib,
  ShieldCheck,
  SignOut,
} from "@phosphor-icons/react";
import { clsx } from "clsx";
import { LanguageSwitcher } from "@/components/ui/language-switcher";
import { useAuth } from "@/features/auth/context/auth-context";
import { useTranslation } from "@/i18n/language-context";
import { uiStyles } from "@/styles/ui.styles";

export default function DashboardLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  const pathname = usePathname();
  const router = useRouter();
  const { user, isAuthenticated, isBootstrapping, logout } = useAuth();
  const { t } = useTranslation();

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

  const navItems = [
    { href: "/overview", label: t.dashboard.nav.overview, icon: ChartBar },
    { href: "/sign", label: t.dashboard.nav.sign, icon: PenNib },
    { href: "/certificates", label: t.dashboard.nav.certificates, icon: Certificate },
    { href: "/api-keys", label: t.dashboard.nav.apiKeys, icon: Key },
    { href: "/documents", label: t.dashboard.nav.documents, icon: Files },
  ];

  const handleLogout = async () => {
    await logout();
    router.replace("/login");
  };

  return (
    <div className={uiStyles.layout.dashboardShell}>
      {/* Desktop Sidebar */}
      <aside className={uiStyles.layout.dashboardSidebar}>
        <div className="space-y-6">
          <Link href="/overview" className="flex items-center gap-3 px-2">
            <div className="flex h-9 w-9 items-center justify-center rounded-xl bg-emerald-500/10 ring-1 ring-emerald-500/25">
              <ShieldCheck size={20} weight="duotone" className="text-emerald-400" />
            </div>
            <span className="text-base font-semibold tracking-tight text-white">
              {t.common.brandName}
            </span>
          </Link>

          <nav className="space-y-1">
            {navItems.map((item) => {
              const Icon = item.icon;
              const active =
                pathname === item.href || pathname.startsWith(`${item.href}/`);
              return (
                <Link
                  key={item.href}
                  href={item.href}
                  className={clsx(
                    uiStyles.nav.itemBase,
                    active ? uiStyles.nav.itemActive : uiStyles.nav.itemInactive
                  )}
                >
                  <Icon size={18} weight={active ? "duotone" : "light"} />
                  <span>{item.label}</span>
                </Link>
              );
            })}
          </nav>

          <div className="pt-4 border-t border-white/[0.06]">
            <Link
              href="/verify"
              className={`${uiStyles.nav.itemBase} ${uiStyles.nav.itemInactive}`}
            >
              <FileMagnifyingGlass size={18} weight="light" />
              <span>{t.dashboard.nav.verifyPublic}</span>
            </Link>
          </div>
        </div>

        <div className="space-y-3 pt-6 border-t border-white/[0.06]">
          <div className="px-2">
            <p className="text-sm font-medium text-white truncate">{user.userName}</p>
            <p className="text-xs text-zinc-500 truncate">{user.email ?? ""}</p>
          </div>
          <div className="flex items-center justify-between gap-2">
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
        </div>
      </aside>

      {/* Mobile Top Header & Horizontal Nav */}
      <header className="lg:hidden sticky top-0 z-20 border-b border-white/[0.06] bg-[#050507]/90 backdrop-blur-xl">
        <div className="flex items-center justify-between px-4 py-3">
          <Link href="/overview" className="flex items-center gap-2.5">
            <ShieldCheck size={22} weight="duotone" className="text-emerald-400" />
            <span className="text-base font-semibold text-white">
              {t.common.brandName}
            </span>
          </Link>
          <div className="flex items-center gap-2">
            <LanguageSwitcher />
            <button
              type="button"
              onClick={handleLogout}
              aria-label={t.common.logout}
              className={uiStyles.button.dangerPill}
            >
              <SignOut size={15} weight="light" />
            </button>
          </div>
        </div>

        <nav className="flex items-center gap-1.5 overflow-x-auto px-4 pb-3">
          {navItems.map((item) => {
            const Icon = item.icon;
            const active =
              pathname === item.href || pathname.startsWith(`${item.href}/`);
            return (
              <Link
                key={item.href}
                href={item.href}
                className={clsx(
                  "inline-flex min-h-[40px] shrink-0 items-center gap-2 rounded-full px-3.5 py-1.5 text-xs font-medium transition-colors",
                  active
                    ? "bg-emerald-500/15 text-emerald-300 ring-1 ring-emerald-500/30"
                    : "bg-white/[0.03] text-zinc-400 ring-1 ring-white/[0.06]"
                )}
              >
                <Icon size={15} weight={active ? "duotone" : "light"} />
                <span>{item.label}</span>
              </Link>
            );
          })}
        </nav>
      </header>

      <main className={uiStyles.layout.dashboardMain}>
        <div className={uiStyles.layout.dashboardContent}>{children}</div>
      </main>
    </div>
  );
}
