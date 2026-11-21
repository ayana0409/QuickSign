import { create } from "zustand";
import {
  decodeJwtUser,
  tokenMemory,
  type DecodedJwtUser,
} from "@/lib/token-memory";
import { authService } from "@/services/auth.service";
import type { LoginRequestDto } from "@/features/auth/types/auth.types";

interface AuthStoreState {
  accessToken: string | null;
  user: DecodedJwtUser | null;
  isAuthenticated: boolean;
  isBootstrapping: boolean;
  setAccessToken: (token: string | null) => void;
  bootstrapSession: () => Promise<void>;
  login: (input: LoginRequestDto) => Promise<void>;
  refreshSession: () => Promise<boolean>;
  logout: () => Promise<void>;
}

export const useAuthStore = create<AuthStoreState>((set, get) => ({
  accessToken: tokenMemory.getAccessToken(),
  user: decodeJwtUser(tokenMemory.getAccessToken()),
  isAuthenticated: Boolean(decodeJwtUser(tokenMemory.getAccessToken())),
  isBootstrapping: true,

  setAccessToken: (token) => {
    const decoded = decodeJwtUser(token);
    set({
      accessToken: token,
      user: decoded,
      isAuthenticated: Boolean(decoded),
    });
  },

  bootstrapSession: async () => {
    if (!tokenMemory.getAccessToken()) {
      await authService.refreshSession();
    }
    const latest = tokenMemory.getAccessToken();
    const decoded = decodeJwtUser(latest);
    set({
      accessToken: latest,
      user: decoded,
      isAuthenticated: Boolean(decoded),
      isBootstrapping: false,
    });
  },

  login: async (input) => {
    const res = await authService.login(input);
    get().setAccessToken(res.access_token);
  },

  refreshSession: async () => {
    const nextToken = await authService.refreshSession();
    get().setAccessToken(nextToken);
    return Boolean(nextToken);
  },

  logout: async () => {
    await authService.logout();
    get().setAccessToken(null);
  },
}));

// Keep Zustand store synchronized whenever dotnetHttpClient or goCryptoHttpClient
// updates tokenMemory during background single-flight refresh.
tokenMemory.subscribe((nextToken) => {
  useAuthStore.getState().setAccessToken(nextToken);
});
