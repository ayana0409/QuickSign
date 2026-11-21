"use client";

import { useEffect } from "react";
import { useRouter } from "next/navigation";
import { CircleNotch } from "@phosphor-icons/react";
import { useAuth } from "@/features/auth/context/auth-context";
import { useTranslation } from "@/i18n/language-context";

export default function HomePage() {
  const router = useRouter();
  const { isAuthenticated, isBootstrapping } = useAuth();
  const { t } = useTranslation();

  useEffect(() => {
    if (!isBootstrapping) {
      router.replace(isAuthenticated ? "/overview" : "/login");
    }
  }, [isBootstrapping, isAuthenticated, router]);

  return (
    <main className="min-h-[100dvh] flex flex-col items-center justify-center gap-3 px-4">
      <CircleNotch size={28} weight="light" className="animate-spin text-emerald-400" />
      <p className="text-sm text-zinc-400">{t.common.checkingSession}</p>
    </main>
  );
}
