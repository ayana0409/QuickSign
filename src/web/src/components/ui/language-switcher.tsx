"use client";

import React from "react";
import { Translate } from "@phosphor-icons/react";
import { useTranslation } from "@/i18n/language-context";
import { uiStyles } from "@/styles/ui.styles";

export function LanguageSwitcher() {
  const { locale, toggleLocale, t } = useTranslation();

  return (
    <button
      type="button"
      onClick={toggleLocale}
      aria-label={t.common.languageLabel}
      className={uiStyles.button.ghostPill}
    >
      <Translate size={15} weight="light" className="text-emerald-400" />
      <span className="font-mono uppercase">{locale}</span>
    </button>
  );
}
