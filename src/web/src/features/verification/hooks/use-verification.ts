"use client";

import { useCallback, useEffect, useRef, useState } from "react";
import type { VerifyResult } from "@/features/verification/types/verification.types";
import { cryptoService } from "@/services/crypto.service";

export type VerifyPhase = "idle" | "checking" | "done" | "error";

interface UseVerificationState {
  phase: VerifyPhase;
  result: VerifyResult | null;
  error: unknown;
}

export function useVerification() {
  const [state, setState] = useState<UseVerificationState>({
    phase: "idle",
    result: null,
    error: null,
  });

  const controllerRef = useRef<AbortController | null>(null);

  const abortActive = useCallback(() => {
    if (controllerRef.current) {
      controllerRef.current.abort();
      controllerRef.current = null;
    }
  }, []);

  useEffect(() => abortActive, [abortActive]);

  const runTask = useCallback(
    async (task: (signal: AbortSignal) => Promise<VerifyResult>) => {
      abortActive();
      const controller = new AbortController();
      controllerRef.current = controller;

      setState({ phase: "checking", result: null, error: null });

      try {
        const result = await task(controller.signal);
        if (!controller.signal.aborted) {
          setState({ phase: "done", result, error: null });
        }
      } catch (err) {
        if (err instanceof DOMException && err.name === "AbortError") {
          return;
        }
        if (!controller.signal.aborted) {
          setState({ phase: "error", result: null, error: err });
        }
      }
    },
    [abortActive]
  );

  const verifyPdf = useCallback(
    (file: File) => runTask((signal) => cryptoService.verifyPdfFile(file, signal)),
    [runTask]
  );

  const verifyJws = useCallback(
    (token: string) => runTask((signal) => cryptoService.verifyJwsToken(token, signal)),
    [runTask]
  );

  const lookupCode = useCallback(
    (code: string) => runTask((signal) => cryptoService.lookupDocumentCode(code, signal)),
    [runTask]
  );

  const reset = useCallback(() => {
    abortActive();
    setState({ phase: "idle", result: null, error: null });
  }, [abortActive]);

  return {
    ...state,
    isChecking: state.phase === "checking",
    verifyPdf,
    verifyJws,
    lookupCode,
    reset,
  };
}
