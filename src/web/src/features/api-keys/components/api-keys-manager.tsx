"use client";

import React, { useCallback, useEffect, useState } from "react";
import {
  CircleNotch,
  Key,
  Plus,
  Trash,
  Warning,
  WarningCircle,
} from "@phosphor-icons/react";
import { CopyButton } from "@/components/ui/copy-button";
import { DoubleBezelCard } from "@/components/ui/double-bezel-card";
import { EmptyState } from "@/components/ui/empty-state";
import { FormField } from "@/components/ui/form-field";
import { IslandButton } from "@/components/ui/island-button";
import { ModalDialog } from "@/components/ui/modal-dialog";
import type { ApiKeyItemDto } from "@/features/api-keys/types/api-key.types";
import { mapErrorMessage } from "@/i18n/map-error-message";
import { useTranslation } from "@/i18n/language-context";
import { apiKeyService } from "@/services/api-key.service";
import { uiStyles } from "@/styles/ui.styles";

export function ApiKeysManager() {
  const { t, locale } = useTranslation();
  const [keys, setKeys] = useState<ApiKeyItemDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [loadError, setLoadError] = useState<unknown>(null);

  // Create modal state
  const [createOpen, setCreateOpen] = useState(false);
  const [name, setName] = useState("");
  const [scopeSign, setScopeSign] = useState(true);
  const [scopeVerify, setScopeVerify] = useState(true);
  const [expiresAt, setExpiresAt] = useState("");
  const [createError, setCreateError] = useState<string | null>(null);
  const [creating, setCreating] = useState(false);

  // One-time plaintext display state (cleared immediately on close)
  const [plaintextKeyOnce, setPlaintextKeyOnce] = useState<string | null>(null);

  // Revoke modal state
  const [revokeTarget, setRevokeTarget] = useState<ApiKeyItemDto | null>(null);
  const [revoking, setRevoking] = useState(false);

  const loadApiKeys = useCallback(async () => {
    setLoading(true);
    setLoadError(null);
    try {
      const list = await apiKeyService.getMyApiKeys();
      setKeys(list);
    } catch (err) {
      setLoadError(err);
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    void loadApiKeys();
  }, [loadApiKeys]);

  const handleCreate = async (e: React.FormEvent) => {
    e.preventDefault();
    setCreateError(null);

    if (!name.trim()) {
      setCreateError(t.dashboard.apiKeys.createModal.nameRequired);
      return;
    }

    const scopesList: string[] = [];
    if (scopeSign) scopesList.push("sign");
    if (scopeVerify) scopesList.push("verify");
    if (scopesList.length === 0) {
      setCreateError(t.dashboard.apiKeys.createModal.scopeRequired);
      return;
    }

    setCreating(true);
    try {
      const created = await apiKeyService.createApiKey({
        name: name.trim(),
        scopes: scopesList.join(","),
        expiresAt: expiresAt ? new Date(expiresAt).toISOString() : null,
      });

      setCreateOpen(false);
      setName("");
      setExpiresAt("");
      setPlaintextKeyOnce(created.plaintextApiKey);
      await loadApiKeys();
    } catch (err) {
      setCreateError(mapErrorMessage(err, t.errors));
    } finally {
      setCreating(false);
    }
  };

  const handleRevoke = async () => {
    if (!revokeTarget) return;
    setRevoking(true);
    try {
      await apiKeyService.revokeApiKey(revokeTarget.id);
      setRevokeTarget(null);
      await loadApiKeys();
    } finally {
      setRevoking(false);
    }
  };

  const formatDate = (iso?: string | null) =>
    iso
      ? new Date(iso).toLocaleDateString(locale === "vi" ? "vi-VN" : "en-US")
      : null;

  return (
    <div className="space-y-6">
      <div className="flex flex-col sm:flex-row sm:items-end justify-between gap-4">
        <div className="space-y-1.5">
          <span className={uiStyles.typography.eyebrowBadge}>
            {t.dashboard.apiKeys.eyebrow}
          </span>
          <h1 className={uiStyles.typography.sectionTitle}>
            {t.dashboard.apiKeys.title}
          </h1>
          <p className={uiStyles.typography.bodyMuted}>
            {t.dashboard.apiKeys.subtitle}
          </p>
        </div>

        <button
          type="button"
          onClick={() => {
            setCreateError(null);
            setCreateOpen(true);
          }}
          className="inline-flex min-h-[44px] items-center gap-2 rounded-full bg-white px-5 py-2 text-xs font-semibold text-zinc-950 transition-colors hover:bg-zinc-100 cursor-pointer self-start sm:self-auto"
        >
          <Plus size={15} weight="bold" />
          <span>{t.dashboard.apiKeys.createButton}</span>
        </button>
      </div>

      {loading && (
        <div className="flex items-center gap-3 py-12 text-sm text-zinc-400">
          <CircleNotch size={20} weight="light" className="animate-spin text-emerald-400" />
          <span>{t.common.loading}</span>
        </div>
      )}

      {loadError !== null && (
        <div className={uiStyles.alert.error} role="alert">
          <WarningCircle size={18} weight="fill" className="shrink-0 mt-0.5" />
          <span>{mapErrorMessage(loadError, t.errors)}</span>
        </div>
      )}

      {!loading && loadError === null && keys.length === 0 && (
        <EmptyState
          icon={<Key size={24} weight="light" />}
          title={t.dashboard.apiKeys.emptyTitle}
          description={t.dashboard.apiKeys.emptyDesc}
          action={
            <button
              type="button"
              onClick={() => setCreateOpen(true)}
              className={uiStyles.button.ghostPill}
            >
              <Plus size={15} weight="light" />
              <span>{t.dashboard.apiKeys.createButton}</span>
            </button>
          }
        />
      )}

      {!loading && keys.length > 0 && (
        <div className="space-y-3">
          {keys.map((item) => (
            <DoubleBezelCard key={item.id}>
              <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
                <div className="space-y-2 min-w-0">
                  <div className="flex flex-wrap items-center gap-2">
                    <h2 className="text-base font-semibold text-white">
                      {item.name}
                    </h2>
                    <span
                      className={
                        item.isActive ? uiStyles.badge.emerald : uiStyles.badge.danger
                      }
                    >
                      {item.isActive
                        ? t.dashboard.apiKeys.statusActive
                        : t.dashboard.apiKeys.statusRevoked}
                    </span>
                  </div>

                  <div className="flex flex-wrap items-center gap-2 text-xs text-zinc-400">
                    <span className={uiStyles.typography.monoCode}>
                      {item.keyPrefix}...
                    </span>
                    <span>•</span>
                    <span>
                      {t.dashboard.apiKeys.fields.scopes}: {item.scopes}
                    </span>
                  </div>

                  <div className="flex flex-wrap items-center gap-3 text-xs text-zinc-500">
                    <span>
                      {t.dashboard.apiKeys.fields.lastUsed}:{" "}
                      {formatDate(item.lastUsedAt) ??
                        t.dashboard.apiKeys.fields.neverUsed}
                    </span>
                    <span>•</span>
                    <span>
                      {t.dashboard.apiKeys.fields.expiresAt}:{" "}
                      {formatDate(item.expiresAt) ??
                        t.dashboard.apiKeys.fields.noExpiry}
                    </span>
                  </div>
                </div>

                {item.isActive && (
                  <button
                    type="button"
                    onClick={() => setRevokeTarget(item)}
                    className={`${uiStyles.button.dangerPill} shrink-0 self-start sm:self-center`}
                  >
                    <Trash size={15} weight="light" />
                    <span>{t.dashboard.apiKeys.fields.revoke}</span>
                  </button>
                )}
              </div>
            </DoubleBezelCard>
          ))}
        </div>
      )}

      {/* Create API Key Modal */}
      <ModalDialog
        open={createOpen}
        onClose={() => setCreateOpen(false)}
        title={t.dashboard.apiKeys.createModal.title}
      >
        <form onSubmit={handleCreate} className="space-y-4">
          {createError && (
            <div className={uiStyles.alert.error} role="alert">
              <WarningCircle size={18} weight="fill" className="shrink-0 mt-0.5" />
              <span>{createError}</span>
            </div>
          )}

          <FormField
            label={t.dashboard.apiKeys.createModal.nameLabel}
            placeholder={t.dashboard.apiKeys.createModal.namePlaceholder}
            value={name}
            onChange={(e) => setName(e.target.value)}
            disabled={creating}
          />

          <div className="space-y-2">
            <p className={uiStyles.input.label}>
              {t.dashboard.apiKeys.createModal.scopesLabel}
            </p>
            <div className="flex flex-wrap gap-3">
              <label className="inline-flex min-h-[44px] items-center gap-2 rounded-xl bg-white/[0.03] px-3.5 py-2 text-xs text-zinc-200 ring-1 ring-white/10 cursor-pointer">
                <input
                  type="checkbox"
                  checked={scopeSign}
                  onChange={(e) => setScopeSign(e.target.checked)}
                  className="accent-emerald-400"
                />
                <span>{t.dashboard.apiKeys.createModal.scopeSign}</span>
              </label>
              <label className="inline-flex min-h-[44px] items-center gap-2 rounded-xl bg-white/[0.03] px-3.5 py-2 text-xs text-zinc-200 ring-1 ring-white/10 cursor-pointer">
                <input
                  type="checkbox"
                  checked={scopeVerify}
                  onChange={(e) => setScopeVerify(e.target.checked)}
                  className="accent-emerald-400"
                />
                <span>{t.dashboard.apiKeys.createModal.scopeVerify}</span>
              </label>
            </div>
          </div>

          <FormField
            label={t.dashboard.apiKeys.createModal.expiresLabel}
            type="datetime-local"
            value={expiresAt}
            onChange={(e) => setExpiresAt(e.target.value)}
            disabled={creating}
          />

          <div className="flex flex-col-reverse sm:flex-row justify-end gap-2.5 pt-2">
            <button
              type="button"
              onClick={() => setCreateOpen(false)}
              className={uiStyles.button.ghostPill}
            >
              {t.dashboard.apiKeys.createModal.cancel}
            </button>
            <IslandButton
              type="submit"
              label={t.dashboard.apiKeys.createModal.submit}
              loadingLabel={t.dashboard.apiKeys.createModal.submitting}
              isLoading={creating}
              className="sm:w-auto"
            />
          </div>
        </form>
      </ModalDialog>

      {/* One-Time Plaintext Key Modal */}
      <ModalDialog
        open={Boolean(plaintextKeyOnce)}
        onClose={() => setPlaintextKeyOnce(null)}
        title={t.dashboard.apiKeys.oneTimeModal.title}
      >
        <div className="space-y-4">
          <div className={uiStyles.alert.warning} role="status">
            <Warning size={18} weight="fill" className="shrink-0 mt-0.5 text-amber-400" />
            <span>{t.dashboard.apiKeys.oneTimeModal.warning}</span>
          </div>

          {plaintextKeyOnce && (
            <div className="rounded-2xl bg-white/[0.03] p-4 ring-1 ring-white/10 space-y-3">
              <p className="font-mono text-xs text-emerald-300 break-all">
                {plaintextKeyOnce}
              </p>
              <CopyButton
                value={plaintextKeyOnce}
                label={t.dashboard.apiKeys.oneTimeModal.copy}
                copiedLabel={t.dashboard.apiKeys.oneTimeModal.copied}
              />
            </div>
          )}

          <div className="flex justify-end">
            <button
              type="button"
              onClick={() => setPlaintextKeyOnce(null)}
              className={uiStyles.button.ghostPill}
            >
              {t.dashboard.apiKeys.oneTimeModal.done}
            </button>
          </div>
        </div>
      </ModalDialog>

      {/* Revoke Confirmation Modal */}
      <ModalDialog
        open={Boolean(revokeTarget)}
        onClose={() => setRevokeTarget(null)}
        title={t.dashboard.apiKeys.revokeModal.title}
      >
        <div className="space-y-4">
          <p className={uiStyles.typography.bodyMuted}>
            {t.dashboard.apiKeys.revokeModal.description}
          </p>

          <div className="flex flex-col-reverse sm:flex-row justify-end gap-2.5">
            <button
              type="button"
              onClick={() => setRevokeTarget(null)}
              className={uiStyles.button.ghostPill}
            >
              {t.dashboard.apiKeys.revokeModal.cancel}
            </button>
            <button
              type="button"
              disabled={revoking}
              onClick={() => void handleRevoke()}
              className={uiStyles.button.dangerPill}
            >
              <span>
                {revoking
                  ? t.dashboard.apiKeys.revokeModal.submitting
                  : t.dashboard.apiKeys.revokeModal.confirm}
              </span>
            </button>
          </div>
        </div>
      </ModalDialog>
    </div>
  );
}
