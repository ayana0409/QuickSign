"use client";

import React, { useId, useState } from "react";
import { Eye, EyeSlash, WarningCircle } from "@phosphor-icons/react";
import { clsx } from "clsx";
import { uiStyles } from "@/styles/ui.styles";
import { useTranslation } from "@/i18n/language-context";

interface FormFieldProps extends Omit<React.InputHTMLAttributes<HTMLInputElement>, "id"> {
  label: string;
  error?: string;
  allowPasswordToggle?: boolean;
}

export function FormField({
  label,
  error,
  type = "text",
  allowPasswordToggle = false,
  className,
  ...rest
}: FormFieldProps) {
  const id = useId();
  const errorId = `${id}-error`;
  const [showPassword, setShowPassword] = useState(false);
  const { t } = useTranslation();

  const resolvedType =
    allowPasswordToggle && type === "password"
      ? showPassword
        ? "text"
        : "password"
      : type;

  return (
    <div className={uiStyles.input.wrapper}>
      <label htmlFor={id} className={uiStyles.input.label}>
        {label}
      </label>
      <div className={uiStyles.input.fieldContainer}>
        <input
          id={id}
          type={resolvedType}
          aria-invalid={Boolean(error)}
          aria-describedby={error ? errorId : undefined}
          className={clsx(
            uiStyles.input.fieldBase,
            allowPasswordToggle && uiStyles.input.fieldWithTrailingIcon,
            error && uiStyles.input.fieldError,
            className
          )}
          {...rest}
        />
        {allowPasswordToggle && type === "password" && (
          <button
            type="button"
            onClick={() => setShowPassword((prev) => !prev)}
            aria-label={showPassword ? t.common.hidePassword : t.common.showPassword}
            className={uiStyles.input.trailingToggleBtn}
          >
            {showPassword ? (
              <EyeSlash size={18} weight="light" />
            ) : (
              <Eye size={18} weight="light" />
            )}
          </button>
        )}
      </div>
      {error && (
        <p id={errorId} role="alert" className={uiStyles.input.errorText}>
          <WarningCircle size={14} weight="regular" className="shrink-0" />
          <span>{error}</span>
        </p>
      )}
    </div>
  );
}
