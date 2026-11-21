import { tokenMemory } from "@/lib/token-memory";
import {
  ApiClientError,
  dotnetHttpClient,
  refreshAccessTokenSingleFlight,
  type UnifiedApiError,
} from "@/services/http/dotnetHttpClient";
import type {
  AuthSessionResponse,
  LoginRequestDto,
  RegisterRequestDto,
} from "@/features/auth/types/auth.types";

export const authService = {
  async login(input: LoginRequestDto): Promise<AuthSessionResponse> {
    let res: Response;
    try {
      res = await fetch("/api/auth/login", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        credentials: "same-origin",
        body: JSON.stringify(input),
        cache: "no-store",
      });
    } catch {
      throw new ApiClientError({
        errorCode: "NETWORK_ERROR",
        httpStatus: 0,
      });
    }

    const text = await res.text();
    const data = text ? (JSON.parse(text) as Record<string, unknown>) : {};

    if (!res.ok) {
      const err = data as Partial<UnifiedApiError>;
      throw new ApiClientError({
        errorCode: err.errorCode || "UNKNOWN_ERROR",
        httpStatus: err.httpStatus || res.status,
        timestamp: err.timestamp,
        traceId: err.traceId,
        details: err.details,
      });
    }

    const authRes = data as unknown as AuthSessionResponse;
    tokenMemory.setAccessToken(authRes.access_token);
    return authRes;
  },

  async register(input: RegisterRequestDto): Promise<void> {
    await dotnetHttpClient.post<void>(
      "/auth/register",
      {
        userName: input.userName,
        emailAddress: input.emailAddress,
        password: input.password,
        appName: input.appName ?? "QuickSign",
      },
      { skipAuth: true }
    );
  },

  async refreshSession(): Promise<string | null> {
    return refreshAccessTokenSingleFlight();
  },

  async logout(): Promise<void> {
    tokenMemory.clear();
    try {
      await fetch("/api/auth/logout", {
        method: "POST",
        credentials: "same-origin",
        cache: "no-store",
      });
    } catch {
      // Ignore network errors on logout after clearing local memory
    }
  },
};
