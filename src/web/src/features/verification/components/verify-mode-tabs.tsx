"use client";

import React from "react";
import { clsx } from "clsx";
import { useTranslation } from "@/i18n/language-context";
import { uiStyles } from "@/styles/ui.styles";

export type VerifyMode = "pdf" | "jws";

interface VerifyModeTabsProps {
  mode: VerifyMode;
  onChange: (mode: VerifyMode) => void;
}

export function VerifyModeTabs({ mode, onChange }: VerifyModeTabsProps) {
  const { t } = useTranslation();

  const handleKeyDown = (e: React.KeyboardEvent<HTMLDivElement>) => {
    if (e.key === "ArrowLeft" || e.key === "ArrowRight") {
      e.preventDefault();
      onChange(mode === "pdf" ? "jws" : "pdf");
    }
  };

  return (
    <div
      role="tablist"
      aria-label={t.verify.page.title}
      onKeyDown={handleKeyDown}
      className={uiStyles.tabs.list}
    >
      <button
        type="button"
        role="tab"
        aria-selected={mode === "pdf"}
        onClick={() => onChange("pdf")}
        className={clsx(
          uiStyles.tabs.tabBase,
          mode === "pdf" ? uiStyles.tabs.tabActive : uiStyles.tabs.tabInactive
        )}
      >
        {t.verify.page.tabs.pdf}
      </button>
      <button
        type="button"
        role="tab"
        aria-selected={mode === "jws"}
        onClick={() => onChange("jws")}
        className={clsx(
          uiStyles.tabs.tabBase,
          mode === "jws" ? uiStyles.tabs.tabActive : uiStyles.tabs.tabInactive
        )}
      >
        {t.verify.page.tabs.jws}
      </button>
    </div>
  );
}
