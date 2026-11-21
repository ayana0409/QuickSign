"use client";

import React, { useId, useState } from "react";
import { useRouter } from "next/navigation";
import { MagnifyingGlass, WarningCircle } from "@phosphor-icons/react";
import {
  isValidDocumentCode,
  normalizeDocumentCode,
} from "@/features/verification/utils/document-code";
import { useTranslation } from "@/i18n/language-context";
import { uiStyles } from "@/styles/ui.styles";

export function DocumentCodeForm() {
  const inputId = useId();
  const router = useRouter();
  const { t } = useTranslation();

  const [code, setCode] = useState("");
  const [error, setError] = useState<string | null>(null);

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);

    const normalized = normalizeDocumentCode(code);
    if (!isValidDocumentCode(normalized)) {
      setError(t.verify.guards.INVALID_DOCUMENT_CODE);
      return;
    }

    router.push(`/verify/${encodeURIComponent(normalized)}`);
  };

  return (
    <form onSubmit={handleSubmit} className="space-y-2.5">
      <label htmlFor={inputId} className={uiStyles.input.label}>
        {t.verify.page.codeLookup.title}
      </label>

      <div className="flex flex-col sm:flex-row gap-2.5">
        <input
          id={inputId}
          type="text"
          value={code}
          onChange={(e) => setCode(e.target.value)}
          placeholder={t.verify.page.codeLookup.placeholder}
          className={`${uiStyles.input.fieldBase} font-mono uppercase`}
        />
        <button type="submit" className={`${uiStyles.button.ghostPill} sm:shrink-0 px-5`}>
          <MagnifyingGlass size={16} weight="light" />
          <span>{t.verify.page.codeLookup.submit}</span>
        </button>
      </div>

      {error && (
        <p role="alert" className={uiStyles.input.errorText}>
          <WarningCircle size={14} weight="regular" className="shrink-0" />
          <span>{error}</span>
        </p>
      )}
    </form>
  );
}
