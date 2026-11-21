import React from "react";
import { clsx } from "clsx";
import { uiStyles } from "@/styles/ui.styles";

interface DoubleBezelCardProps {
  children: React.ReactNode;
  className?: string;
  innerClassName?: string;
}

export function DoubleBezelCard({
  children,
  className,
  innerClassName,
}: DoubleBezelCardProps) {
  return (
    <div className={clsx(uiStyles.card.doubleBezelOuter, className)}>
      <div className={clsx(uiStyles.card.doubleBezelInner, innerClassName)}>
        {children}
      </div>
    </div>
  );
}
