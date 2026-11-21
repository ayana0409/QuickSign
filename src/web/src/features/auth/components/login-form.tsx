"use client";

import React, { useState } from "react";
import Link from "next/link";
import { useRouter, useSearchParams } from "next/navigation";
import { CheckCircle, WarningCircle } from "@phosphor-icons/react";
import { DoubleBezelCard } from "@/components/ui/double-bezel-card";
import { FormField } from "@/components/ui/form-field";
import { IslandButton } from "@/components/ui/island-button";
import { useAuth } from "@/features/auth/context/auth-context";
import { mapAuthErrorMessage } from "@/features/auth/utils/auth-error-mapper";
import { useTranslation } from "@/i18n/language-context";
import { uiStyles } from "@/styles/ui.styles";

export function LoginForm() {
  const router = useRouter();
  const searchParams = useSearchParams();
  const justRegistered = searchParams.get("registered") === "1";

  const { login } = useAuth();
  const { t } = useTranslation();

  const [usernameOrEmail, setUsernameOrEmail] = useState("");
  const [password, setPassword] = useState("");
  const [fieldErrors, setFieldErrors] = useState<{
    usernameOrEmail?: string;
    password?: string;
  }>({});
  const [serverError, setServerError] = useState<string | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setServerError(null);

    const nextErrors: { usernameOrEmail?: string; password?: string } = {};
    if (!usernameOrEmail.trim()) {
      nextErrors.usernameOrEmail = t.auth.login.validation.usernameRequired;
    }
    if (!password) {
      nextErrors.password = t.auth.login.validation.passwordRequired;
    }

    if (Object.keys(nextErrors).length > 0) {
      setFieldErrors(nextErrors);
      return;
    }

    setFieldErrors({});
    setIsSubmitting(true);
    try {
      await login({
        usernameOrEmail: usernameOrEmail.trim(),
        password,
      });
      router.replace("/overview");
    } catch (err) {
      setServerError(mapAuthErrorMessage(err, t.errors));
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <DoubleBezelCard>
      <div className="space-y-6">
        <div className="space-y-2">
          <h1 className={uiStyles.typography.sectionTitle}>{t.auth.login.title}</h1>
          <p className={uiStyles.typography.bodyMuted}>{t.auth.login.subtitle}</p>
        </div>

        {justRegistered && (
          <div className={uiStyles.alert.success} role="status">
            <CheckCircle size={18} weight="fill" className="shrink-0 mt-0.5" />
            <span>{t.auth.login.registeredBanner}</span>
          </div>
        )}

        {serverError && (
          <div className={uiStyles.alert.error} role="alert">
            <WarningCircle size={18} weight="fill" className="shrink-0 mt-0.5" />
            <span>{serverError}</span>
          </div>
        )}

        <form onSubmit={handleSubmit} noValidate className="space-y-4">
          <FormField
            label={t.auth.login.usernameLabel}
            name="usernameOrEmail"
            type="text"
            autoComplete="username"
            placeholder={t.auth.login.usernamePlaceholder}
            value={usernameOrEmail}
            onChange={(e) => setUsernameOrEmail(e.target.value)}
            error={fieldErrors.usernameOrEmail}
            disabled={isSubmitting}
          />

          <FormField
            label={t.auth.login.passwordLabel}
            name="password"
            type="password"
            autoComplete="current-password"
            placeholder={t.auth.login.passwordPlaceholder}
            value={password}
            onChange={(e) => setPassword(e.target.value)}
            error={fieldErrors.password}
            allowPasswordToggle
            disabled={isSubmitting}
          />

          <div className="pt-2">
            <IslandButton
              type="submit"
              label={t.auth.login.submitButton}
              loadingLabel={t.auth.login.submittingButton}
              isLoading={isSubmitting}
            />
          </div>
        </form>

        <p className="text-center text-xs text-zinc-400">
          {t.auth.login.noAccount}{" "}
          <Link
            href="/register"
            className="font-medium text-emerald-400 hover:text-emerald-300 transition-colors"
          >
            {t.auth.login.registerLink}
          </Link>
        </p>
      </div>
    </DoubleBezelCard>
  );
}
