"use client";

import React, { useId, useState } from "react";
import { FileText, WarningCircle } from "@phosphor-icons/react";
import { IslandButton } from "@/components/ui/island-button";
import { extractJwsToken } from "@/features/verification/utils/jws-input";
import { MAX_VERIFY_FILE_BYTES } from "@/features/verification/utils/file-guard";
import { useTranslation } from "@/i18n/language-context";
import { uiStyles } from "@/styles/ui.styles";

interface JwsInputPanelProps {
  isChecking: boolean;
  onSubmitJws: (token: string) => void;
}

export function JwsInputPanel({ isChecking, onSubmitJws }: JwsInputPanelProps) {
  const textareaId = useId();
  const fileInputId = useId();
  const { t } = useTranslation();

  const [rawText, setRawText] = useState("");
  const [guardError, setGuardError] = useState<string | null>(null);

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    setGuardError(null);

    const token = extractJwsToken(rawText);
    if (!token) {
      setGuardError(t.verify.guards.INVALID_JWS_FORMAT);
      return;
    }

    onSubmitJws(token);
  };

  const handleLoadTextFile = async (file: File | undefined) => {
    if (!file) return;
    setGuardError(null);

    if (file.size === 0) {
      setGuardError(t.verify.guards.FILE_EMPTY);
      return;
    }
    if (file.size > MAX_VERIFY_FILE_BYTES) {
      setGuardError(t.verify.guards.FILE_TOO_LARGE);
      return;
    }

    const content = await file.text();
    setRawText(content);
  };

  return (
    <form onSubmit={handleSubmit} className="space-y-4">
      <div className={uiStyles.input.wrapper}>
        <div className="flex flex-wrap items-center justify-between gap-2">
          <label htmlFor={textareaId} className={uiStyles.input.label}>
            {t.verify.page.jws.label}
          </label>
          <label htmlFor={fileInputId} className={uiStyles.button.ghostPill}>
            <FileText size={14} weight="light" />
            <span>{t.verify.page.jws.loadFile}</span>
            <input
              id={fileInputId}
              type="file"
              accept=".txt,.json,.jws,application/json,text/plain"
              className="sr-only"
              onChange={(e) => {
                void handleLoadTextFile(e.target.files?.[0]);
                e.target.value = "";
              }}
            />
          </label>
        </div>

        <textarea
          id={textareaId}
          rows={5}
          value={rawText}
          onChange={(e) => setRawText(e.target.value)}
          placeholder={t.verify.page.jws.placeholder}
          disabled={isChecking}
          className={`${uiStyles.input.fieldBase} font-mono text-xs leading-relaxed`}
        />
      </div>

      {guardError && (
        <div className={uiStyles.alert.error} role="alert">
          <WarningCircle size={18} weight="fill" className="shrink-0 mt-0.5" />
          <span>{guardError}</span>
        </div>
      )}

      <IslandButton
        type="submit"
        label={t.verify.page.jws.submit}
        loadingLabel={t.verify.page.jws.submitting}
        isLoading={isChecking}
      />
    </form>
  );
}
