"use client";

import React, { useId, useState } from "react";
import { CircleNotch, FilePdf, UploadSimple, WarningCircle } from "@phosphor-icons/react";
import { clsx } from "clsx";
import { validatePdfFile } from "@/features/verification/utils/file-guard";
import { useTranslation } from "@/i18n/language-context";
import { uiStyles } from "@/styles/ui.styles";

interface PdfDropzoneProps {
  isChecking: boolean;
  onSelectFile: (file: File) => void;
}

export function PdfDropzone({ isChecking, onSelectFile }: PdfDropzoneProps) {
  const inputId = useId();
  const { t } = useTranslation();

  const [isDragging, setIsDragging] = useState(false);
  const [selectedName, setSelectedName] = useState<string | null>(null);
  const [guardError, setGuardError] = useState<string | null>(null);

  const handleFile = async (file: File | undefined) => {
    if (!file || isChecking) {
      return;
    }

    setGuardError(null);
    const errKey = await validatePdfFile(file);
    if (errKey) {
      setSelectedName(null);
      setGuardError(t.verify.guards[errKey]);
      return;
    }

    setSelectedName(file.name);
    onSelectFile(file);
  };

  return (
    <div className="space-y-3">
      <label
        htmlFor={inputId}
        onDragOver={(e) => {
          e.preventDefault();
          if (!isChecking) setIsDragging(true);
        }}
        onDragLeave={() => setIsDragging(false)}
        onDrop={(e) => {
          e.preventDefault();
          setIsDragging(false);
          void handleFile(e.dataTransfer.files?.[0]);
        }}
        className={clsx(
          uiStyles.dropzone.base,
          isDragging && uiStyles.dropzone.dragging,
          isChecking && "pointer-events-none opacity-60"
        )}
      >
        <input
          id={inputId}
          type="file"
          accept="application/pdf,.pdf"
          disabled={isChecking}
          className="sr-only"
          onChange={(e) => {
            void handleFile(e.target.files?.[0]);
            e.target.value = "";
          }}
        />

        <div className={uiStyles.dropzone.iconWrap}>
          {isChecking ? (
            <CircleNotch size={24} weight="bold" className="animate-spin" />
          ) : selectedName ? (
            <FilePdf size={24} weight="duotone" />
          ) : (
            <UploadSimple size={24} weight="light" />
          )}
        </div>

        <div className="space-y-1">
          <p className="text-sm font-medium text-white">
            {isChecking ? t.common.loading : t.verify.page.pdf.dropTitle}
          </p>
          <p className="text-xs text-zinc-400">
            {selectedName
              ? `${t.verify.page.pdf.selectedFile} ${selectedName}`
              : t.verify.page.pdf.dropHint}
          </p>
        </div>
      </label>

      {guardError && (
        <div className={uiStyles.alert.error} role="alert">
          <WarningCircle size={18} weight="fill" className="shrink-0 mt-0.5" />
          <span>{guardError}</span>
        </div>
      )}
    </div>
  );
}
