"use client";

import React, { useEffect } from "react";
import { useAuthStore } from "@/stores/auth-store";

export function AuthProvider({ children }: { children: React.ReactNode }) {
  const bootstrapSession = useAuthStore((s) => s.bootstrapSession);

  useEffect(() => {
    void bootstrapSession();
  }, [bootstrapSession]);

  return <>{children}</>;
}

export function useAuth() {
  return useAuthStore();
}
