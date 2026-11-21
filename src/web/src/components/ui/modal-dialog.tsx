"use client";

import React, { useEffect, useRef } from "react";
import { X } from "@phosphor-icons/react";
import { DoubleBezelCard } from "@/components/ui/double-bezel-card";
import { uiStyles } from "@/styles/ui.styles";

interface ModalDialogProps {
  open: boolean;
  onClose: () => void;
  title: string;
  children: React.ReactNode;
}

export function ModalDialog({
  open,
  onClose,
  title,
  children,
}: ModalDialogProps) {
  const dialogRef = useRef<HTMLDialogElement | null>(null);

  useEffect(() => {
    const el = dialogRef.current;
    if (!el) return;

    if (open && !el.open) {
      el.showModal();
    } else if (!open && el.open) {
      el.close();
    }
  }, [open]);

  if (!open) {
    return null;
  }

  return (
    <dialog
      ref={dialogRef}
      onCancel={(e) => {
        e.preventDefault();
        onClose();
      }}
      onClick={(e) => {
        if (e.target === dialogRef.current) {
          onClose();
        }
      }}
      className={uiStyles.modal.dialog}
    >
      <DoubleBezelCard>
        <div className="space-y-5">
          <div className="flex items-center justify-between gap-4">
            <h2 className="text-lg font-semibold text-white">{title}</h2>
            <button
              type="button"
              onClick={onClose}
              aria-label="Close"
              className="flex h-11 w-11 items-center justify-center rounded-full bg-white/[0.04] text-zinc-400 ring-1 ring-white/10 transition-colors hover:bg-white/[0.08] hover:text-white cursor-pointer"
            >
              <X size={18} weight="light" />
            </button>
          </div>
          {children}
        </div>
      </DoubleBezelCard>
    </dialog>
  );
}
