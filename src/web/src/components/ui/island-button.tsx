"use client";

import React from "react";
import { ArrowUpRight, CircleNotch } from "@phosphor-icons/react";
import { clsx } from "clsx";
import { uiStyles } from "@/styles/ui.styles";

interface IslandButtonProps extends React.ButtonHTMLAttributes<HTMLButtonElement> {
  label: string;
  isLoading?: boolean;
  loadingLabel?: string;
}

export function IslandButton({
  label,
  isLoading = false,
  loadingLabel,
  className,
  disabled,
  type = "button",
  ...rest
}: IslandButtonProps) {
  return (
    <button
      type={type}
      disabled={disabled || isLoading}
      className={clsx(uiStyles.button.islandPrimary, className)}
      {...rest}
    >
      <span>{isLoading && loadingLabel ? loadingLabel : label}</span>
      <span className={uiStyles.button.islandIconWrap} aria-hidden="true">
        {isLoading ? (
          <CircleNotch size={16} weight="bold" className="animate-spin" />
        ) : (
          <ArrowUpRight size={16} weight="light" />
        )}
      </span>
    </button>
  );
}
