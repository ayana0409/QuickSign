"use client";

import React from "react";
import { Warning } from "@phosphor-icons/react";
import { useTranslation } from "@/i18n/language-context";
import { uiStyles } from "@/styles/ui.styles";

export function SecurityNoticeBanner() {
  const { t } = useTranslation();

  return (
    <div className={uiStyles.alert.warning} role="status">
      <Warning size={20} weight="fill" className="shrink-0 mt-0.5 text-amber-400" />
      <div className="space-y-1">
        <p className="font-semibold text-amber-200">{t.verify.securityNotice.title}</p>
        <p className="text-amber-200/90 leading-relaxed">{t.verify.securityNotice.body}</p>
      </div>
    </div>
  );
}
