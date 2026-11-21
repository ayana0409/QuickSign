"use client";

import React, { useState } from "react";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { WarningCircle } from "@phosphor-icons/react";
import { DoubleBezelCard } from "@/components/ui/double-bezel-card";
import { FormField } from "@/components/ui/form-field";
import { IslandButton } from "@/components/ui/island-button";
import {
  isPasswordStrong,
  PasswordStrength,
} from "@/features/auth/components/password-strength";
import { mapErrorMessage } from "@/i18n/map-error-message";
import { useTranslation } from "@/i18n/language-context";
import { authService } from "@/services/auth.service";
import { uiStyles } from "@/styles/ui.styles";

export function RegisterForm() {
  const router = useRouter();
  const { t } = useTranslation();

  const [userName, setUserName] = useState("");
  const [emailAddress, setEmailAddress] = useState("");
  const [password, setPassword] = useState("");
  const [confirmPassword, setConfirmPassword] = useState("");
  const [fieldErrors, setFieldErrors] = useState<{
    userName?: string;
    emailAddress?: string;
    password?: string;
    confirmPassword?: string;
  }>({});
  const [serverError, setServerError] = useState<string | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setServerError(null);

    const nextErrors: typeof fieldErrors = {};
    const trimmedUser = userName.trim();
    const trimmedEmail = emailAddress.trim();

    if (!trimmedUser) {
      nextErrors.userName = t.auth.register.validation.usernameRequired;
    } else if (/\s/.test(trimmedUser)) {
      nextErrors.userName = t.auth.register.validation.usernameInvalid;
    }

    if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(trimmedEmail)) {
      nextErrors.emailAddress = t.auth.register.validation.emailInvalid;
    }

    if (!isPasswordStrong(password)) {
      nextErrors.password = t.auth.register.validation.passwordWeak;
    }

    if (password !== confirmPassword) {
      nextErrors.confirmPassword = t.auth.register.validation.passwordMismatch;
    }

    if (Object.keys(nextErrors).length > 0) {
      setFieldErrors(nextErrors);
      return;
    }

    setFieldErrors({});
    setIsSubmitting(true);
    try {
      await authService.register({
        userName: trimmedUser,
        emailAddress: trimmedEmail,
        password,
        appName: "QuickSign",
      });
      router.replace("/login?registered=1");
    } catch (err) {
      setServerError(mapErrorMessage(err, t.errors));
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <DoubleBezelCard>
      <div className="space-y-6">
        <div className="space-y-2">
          <h1 className={uiStyles.typography.sectionTitle}>{t.auth.register.title}</h1>
          <p className={uiStyles.typography.bodyMuted}>{t.auth.register.subtitle}</p>
        </div>

        {serverError && (
          <div className={uiStyles.alert.error} role="alert">
            <WarningCircle size={18} weight="fill" className="shrink-0 mt-0.5" />
            <span>{serverError}</span>
          </div>
        )}

        <form onSubmit={handleSubmit} noValidate className="space-y-4">
          <FormField
            label={t.auth.register.usernameLabel}
            name="userName"
            type="text"
            autoComplete="username"
            placeholder={t.auth.register.usernamePlaceholder}
            value={userName}
            onChange={(e) => setUserName(e.target.value)}
            error={fieldErrors.userName}
            disabled={isSubmitting}
          />

          <FormField
            label={t.auth.register.emailLabel}
            name="emailAddress"
            type="email"
            autoComplete="email"
            placeholder={t.auth.register.emailPlaceholder}
            value={emailAddress}
            onChange={(e) => setEmailAddress(e.target.value)}
            error={fieldErrors.emailAddress}
            disabled={isSubmitting}
          />

          <FormField
            label={t.auth.register.passwordLabel}
            name="password"
            type="password"
            autoComplete="new-password"
            placeholder={t.auth.register.passwordPlaceholder}
            value={password}
            onChange={(e) => setPassword(e.target.value)}
            error={fieldErrors.password}
            allowPasswordToggle
            disabled={isSubmitting}
          />

          <PasswordStrength password={password} />

          <FormField
            label={t.auth.register.confirmPasswordLabel}
            name="confirmPassword"
            type="password"
            autoComplete="new-password"
            placeholder={t.auth.register.confirmPasswordPlaceholder}
            value={confirmPassword}
            onChange={(e) => setConfirmPassword(e.target.value)}
            error={fieldErrors.confirmPassword}
            allowPasswordToggle
            disabled={isSubmitting}
          />

          <div className="pt-2">
            <IslandButton
              type="submit"
              label={t.auth.register.submitButton}
              loadingLabel={t.auth.register.submittingButton}
              isLoading={isSubmitting}
            />
          </div>
        </form>

        <p className="text-center text-xs text-zinc-400">
          {t.auth.register.hasAccount}{" "}
          <Link
            href="/login"
            className="font-medium text-emerald-400 hover:text-emerald-300 transition-colors"
          >
            {t.auth.register.loginLink}
          </Link>
        </p>
      </div>
    </DoubleBezelCard>
  );
}
