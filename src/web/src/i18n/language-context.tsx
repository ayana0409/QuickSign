"use client";

import React, { createContext, useCallback, useContext, useMemo, useState } from "react";
import viCommon from "@/locales/vi/common.json";
import viAuth from "@/locales/vi/auth.json";
import viErrors from "@/locales/vi/errors.json";
import enCommon from "@/locales/en/common.json";
import enAuth from "@/locales/en/auth.json";
import enErrors from "@/locales/en/errors.json";
import type { Locale, TranslationDictionary } from "./types";

const DICTIONARIES: Record<Locale, TranslationDictionary> = {
  vi: {
    common: viCommon,
    auth: viAuth,
    errors: viErrors,
  },
  en: {
    common: enCommon,
    auth: enAuth,
    errors: enErrors,
  },
};

interface LanguageContextValue {
  locale: Locale;
  setLocale: (next: Locale) => void;
  toggleLocale: () => void;
  t: TranslationDictionary;
}

const LanguageContext = createContext<LanguageContextValue | null>(null);

export function LanguageProvider({
  children,
  initialLocale = "vi",
}: {
  children: React.ReactNode;
  initialLocale?: Locale;
}) {
  const [locale, setLocaleState] = useState<Locale>(initialLocale);

  const setLocale = useCallback((next: Locale) => {
    setLocaleState(next);
    if (typeof document !== "undefined") {
      document.documentElement.lang = next;
    }
  }, []);

  const toggleLocale = useCallback(() => {
    setLocaleState((prev) => {
      const next = prev === "vi" ? "en" : "vi";
      if (typeof document !== "undefined") {
        document.documentElement.lang = next;
      }
      return next;
    });
  }, []);

  const value = useMemo<LanguageContextValue>(
    () => ({
      locale,
      setLocale,
      toggleLocale,
      t: DICTIONARIES[locale],
    }),
    [locale, setLocale, toggleLocale]
  );

  return <LanguageContext.Provider value={value}>{children}</LanguageContext.Provider>;
}

export function useTranslation(): LanguageContextValue {
  const ctx = useContext(LanguageContext);
  if (!ctx) {
    throw new Error("useTranslation must be used within a LanguageProvider.");
  }
  return ctx;
}
