"use client";

import React from "react";
import { CheckCircle, Circle } from "@phosphor-icons/react";
import { useTranslation } from "@/i18n/language-context";

export function evaluatePasswordCriteria(password: string) {
  return {
    minLength: password.length >= 6,
    hasUppercase: /[A-Z]/.test(password),
    hasNumber: /[0-9]/.test(password),
    hasSpecial: /[^A-Za-z0-9]/.test(password),
  };
}

export function isPasswordStrong(password: string): boolean {
  const c = evaluatePasswordCriteria(password);
  return c.minLength && c.hasUppercase && c.hasNumber && c.hasSpecial;
}

export function PasswordStrength({ password }: { password: string }) {
  const { t } = useTranslation();
  const c = evaluatePasswordCriteria(password);

  const items = [
    { key: "minLength", met: c.minLength, label: t.auth.register.strength.minLength },
    { key: "hasUppercase", met: c.hasUppercase, label: t.auth.register.strength.uppercase },
    { key: "hasNumber", met: c.hasNumber, label: t.auth.register.strength.number },
    { key: "hasSpecial", met: c.hasSpecial, label: t.auth.register.strength.special },
  ];

  return (
    <div className="rounded-2xl bg-white/[0.02] p-3.5 ring-1 ring-white/[0.06] space-y-2">
      <p className="text-[11px] font-medium uppercase tracking-wider text-zinc-400">
        {t.auth.register.strength.title}
      </p>
      <ul className="grid grid-cols-1 sm:grid-cols-2 gap-1.5">
        {items.map((item) => (
          <li
            key={item.key}
            className={`flex items-center gap-2 text-xs transition-colors duration-300 ${
              item.met ? "text-emerald-400" : "text-zinc-500"
            }`}
          >
            {item.met ? (
              <CheckCircle size={14} weight="fill" className="shrink-0" />
            ) : (
              <Circle size={14} weight="light" className="shrink-0" />
            )}
            <span>{item.label}</span>
          </li>
        ))}
      </ul>
    </div>
  );
}
