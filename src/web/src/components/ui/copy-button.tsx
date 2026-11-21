"use client";

import React, { useState } from "react";
import { Check, Copy } from "@phosphor-icons/react";
import { uiStyles } from "@/styles/ui.styles";

interface CopyButtonProps {
  value: string;
  label: string;
  copiedLabel: string;
}

export function CopyButton({ value, label, copiedLabel }: CopyButtonProps) {
  const [copied, setCopied] = useState(false);

  const handleCopy = async () => {
    try {
      await navigator.clipboard.writeText(value);
      setCopied(true);
      setTimeout(() => setCopied(false), 1800);
    } catch {
      // Clipboard API may be unavailable in non-HTTPS contexts
    }
  };

  return (
    <button
      type="button"
      onClick={handleCopy}
      className={uiStyles.button.ghostPill}
      aria-label={copied ? copiedLabel : label}
    >
      {copied ? (
        <Check size={14} weight="bold" className="text-emerald-400" />
      ) : (
        <Copy size={14} weight="light" />
      )}
      <span>{copied ? copiedLabel : label}</span>
    </button>
  );
}
