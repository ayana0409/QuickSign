"use client";

import React from "react";
import {
  CaretDown,
  CheckCircle,
  Question,
  ShieldWarning,
  XCircle,
} from "@phosphor-icons/react";
import { CopyButton } from "@/components/ui/copy-button";
import type { VerifyResult } from "@/features/verification/types/verification.types";
import { getVerificationViewModel } from "@/features/verification/utils/verification-view-model";
import { useTranslation } from "@/i18n/language-context";
import { uiStyles } from "@/styles/ui.styles";

interface VerificationResultCardProps {
  result: VerifyResult;
  onReset?: () => void;
}

export function VerificationResultCard({
  result,
  onReset,
}: VerificationResultCardProps) {
  const { t, locale } = useTranslation();
  const vm = getVerificationViewModel(result.validationStatus);
  const statusCopy = t.verify.result.statuses[vm.statusKey];

  const Icon =
    vm.tone === "valid"
      ? CheckCircle
      : vm.tone === "danger"
      ? XCircle
      : vm.tone === "warning"
      ? ShieldWarning
      : Question;

  const iconColor =
    vm.tone === "valid"
      ? "text-emerald-400"
      : vm.tone === "danger"
      ? "text-rose-400"
      : vm.tone === "warning"
      ? "text-amber-400"
      : "text-zinc-400";

  const formattedTime = result.signingTime
    ? new Date(result.signingTime).toLocaleString(locale === "vi" ? "vi-VN" : "en-US")
    : "-";

  return (
    <div className={uiStyles.resultCard[vm.tone]} role="status">
      <div className="flex items-start gap-3">
        <Icon size={24} weight="fill" className={`shrink-0 mt-0.5 ${iconColor}`} />
        <div className="space-y-1">
          <h2 className="text-base font-semibold text-white">{statusCopy.title}</h2>
          <p className="text-xs sm:text-sm text-zinc-300 leading-relaxed">
            {statusCopy.description}
          </p>
        </div>
      </div>

      <div className="grid grid-cols-1 sm:grid-cols-2 gap-2.5">
        {result.signerSubject && (
          <div className={uiStyles.card.metricTile}>
            <p className="text-[11px] uppercase tracking-wider text-zinc-500">
              {t.verify.result.fields.signer}
            </p>
            <p className="mt-1 text-sm font-medium text-white break-words">
              {result.signerSubject}
            </p>
          </div>
        )}

        {result.signingTime && (
          <div className={uiStyles.card.metricTile}>
            <p className="text-[11px] uppercase tracking-wider text-zinc-500">
              {t.verify.result.fields.signingTime}
            </p>
            <p className="mt-1 text-sm text-zinc-200">{formattedTime}</p>
          </div>
        )}

        {result.documentName && (
          <div className={uiStyles.card.metricTile}>
            <p className="text-[11px] uppercase tracking-wider text-zinc-500">
              {t.verify.result.fields.documentName}
            </p>
            <p className="mt-1 text-sm text-zinc-200 break-words">
              {result.documentName}
            </p>
          </div>
        )}

        <div className={uiStyles.card.metricTile}>
          <p className="text-[11px] uppercase tracking-wider text-zinc-500">
            {t.verify.result.fields.revocationChecked}
          </p>
          <p className="mt-1 text-sm text-zinc-200">
            {result.revocationChecked
              ? t.verify.result.fields.checkedYes
              : t.verify.result.fields.checkedNo}
          </p>
        </div>
      </div>

      {(result.documentCode || result.sha256Hash) && (
        <details className={uiStyles.detailsDrawer.root}>
          <summary className={uiStyles.detailsDrawer.summary}>
            <span>{t.verify.result.fields.detailsToggle}</span>
            <CaretDown size={14} weight="light" />
          </summary>
          <div className={uiStyles.detailsDrawer.body}>
            {result.documentCode && (
              <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-2.5">
                <div className="space-y-0.5 min-w-0">
                  <p className="text-[11px] uppercase tracking-wider text-zinc-500">
                    {t.verify.result.fields.documentCode}
                  </p>
                  <p className="font-mono text-xs text-zinc-200 break-all">
                    {result.documentCode}
                  </p>
                </div>
                <div className="shrink-0">
                  <CopyButton
                    value={result.documentCode}
                    label={t.verify.result.fields.copy}
                    copiedLabel={t.verify.result.fields.copied}
                  />
                </div>
              </div>
            )}

            {result.sha256Hash && (
              <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-2.5 pt-2.5 border-t border-white/[0.05]">
                <div className="space-y-0.5 min-w-0">
                  <p className="text-[11px] uppercase tracking-wider text-zinc-500">
                    {t.verify.result.fields.hash}
                  </p>
                  <p className="font-mono text-xs text-zinc-300 break-all">
                    {result.sha256Hash}
                  </p>
                </div>
                <div className="shrink-0">
                  <CopyButton
                    value={result.sha256Hash}
                    label={t.verify.result.fields.copy}
                    copiedLabel={t.verify.result.fields.copied}
                  />
                </div>
              </div>
            )}
          </div>
        </details>
      )}

      {onReset && (
        <div className="pt-1">
          <button
            type="button"
            onClick={onReset}
            className={uiStyles.button.ghostPill}
          >
            {t.verify.result.reset}
          </button>
        </div>
      )}
    </div>
  );
}
