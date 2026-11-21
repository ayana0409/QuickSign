"use client";

import React, {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useMemo,
  useState,
} from "react";
import {
  decodeJwtUser,
  tokenMemory,
  type DecodedJwtUser,
} from "@/lib/token-memory";
import { authService } from "@/services/auth.service";
import type { LoginRequestDto } from "@/features/auth/types/auth.types";

interface AuthContextValue {
  user: DecodedJwtUser | null;
  accessToken: string | null;
  isAuthenticated: boolean;
  isBootstrapping: boolean;
  login: (input: LoginRequestDto) => Promise<void>;
  refreshSession: () => Promise<boolean>;
  logout: () => Promise<void>;
}

const AuthContext = createContext<AuthContextValue | null>(null);

export function AuthProvider({ children }: { children: React.ReactNode }) {
  const [accessToken, setAccessToken] = useState<string | null>(() =>
    tokenMemory.getAccessToken()
  );
  const [isBootstrapping, setIsBootstrapping] = useState<boolean>(true);

  useEffect(() => {
    const unsubscribe = tokenMemory.subscribe((nextToken) => {
      setAccessToken(nextToken);
    });

    // Attempt silent session restoration via HttpOnly refresh cookie on mount
    let cancelled = false;
    (async () => {
      if (!tokenMemory.getAccessToken()) {
        await authService.refreshSession();
      }
      if (!cancelled) {
        setIsBootstrapping(false);
      }
    })();

    return () => {
      cancelled = true;
      unsubscribe();
    };
  }, []);

  const user = useMemo(() => decodeJwtUser(accessToken), [accessToken]);

  const login = useCallback(async (input: LoginRequestDto) => {
    await authService.login(input);
  }, []);

  const refreshSession = useCallback(async () => {
    const nextToken = await authService.refreshSession();
    return Boolean(nextToken);
  }, []);

  const logout = useCallback(async () => {
    await authService.logout();
  }, []);

  const value = useMemo<AuthContextValue>(
    () => ({
      user,
      accessToken,
      isAuthenticated: Boolean(user),
      isBootstrapping,
      login,
      refreshSession,
      logout,
    }),
    [user, accessToken, isBootstrapping, login, refreshSession, logout]
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth(): AuthContextValue {
  const ctx = useContext(AuthContext);
  if (!ctx) {
    throw new Error("useAuth must be used within an AuthProvider.");
  }
  return ctx;
}
