import { tokenMemory } from "@/lib/token-memory";
import {
  ApiClientError,
  parseUnifiedErrorResponse,
  type UnifiedApiError,
} from "@/services/http/api-error";

export { ApiClientError, type UnifiedApiError };

const DOTNET_BASE_URL =
  process.env.NEXT_PUBLIC_DOTNET_API_URL ?? "http://localhost:5001/api/v1";

// Single-flight promise prevents duplicate /api/auth/refresh calls when
// multiple concurrent requests receive 401 simultaneously.
let activeRefreshPromise: Promise<string | null> | null = null;

export async function refreshAccessTokenSingleFlight(): Promise<string | null> {
  if (activeRefreshPromise) {
    return activeRefreshPromise;
  }

  activeRefreshPromise = (async () => {
    try {
      const res = await fetch("/api/auth/refresh", {
        method: "POST",
        credentials: "same-origin",
        cache: "no-store",
      });

      if (!res.ok) {
        tokenMemory.clear();
        return null;
      }

      const data = (await res.json()) as { access_token?: string };
      if (!data.access_token) {
        tokenMemory.clear();
        return null;
      }

      tokenMemory.setAccessToken(data.access_token);
      return data.access_token;
    } catch {
      return null;
    } finally {
      activeRefreshPromise = null;
    }
  })();

  return activeRefreshPromise;
}

interface RequestOptions {
  skipAuth?: boolean;
  _isRetry?: boolean;
}

async function parseErrorResponse(res: Response): Promise<UnifiedApiError> {
  try {
    const json = (await res.json()) as Partial<UnifiedApiError>;
    return {
      errorCode: json.errorCode || "UNKNOWN_ERROR",
      httpStatus: json.httpStatus || res.status,
      timestamp: json.timestamp,
      traceId: json.traceId,
      message: json.message,
      details: json.details,
    };
  } catch {
    return {
      errorCode: "UNKNOWN_ERROR",
      httpStatus: res.status,
    };
  }
}

async function request<T>(
  method: string,
  path: string,
  body?: unknown,
  options: RequestOptions = {}
): Promise<T> {
  const headers: Record<string, string> = {
    "Content-Type": "application/json",
  };

  if (!options.skipAuth) {
    const token = tokenMemory.getAccessToken();
    if (token) {
      headers.Authorization = `Bearer ${token}`;
    }
  }

  let res: Response;
  try {
    res = await fetch(`${DOTNET_BASE_URL}${path}`, {
      method,
      headers,
      body: body !== undefined ? JSON.stringify(body) : undefined,
      cache: "no-store",
    });
  } catch {
    throw new ApiClientError({
      errorCode: "NETWORK_ERROR",
      httpStatus: 0,
    });
  }

  if (res.status === 401 && !options.skipAuth && !options._isRetry) {
    const newToken = await refreshAccessTokenSingleFlight();
    if (newToken) {
      return request<T>(method, path, body, { ...options, _isRetry: true });
    }
  }

  if (!res.ok) {
    const errPayload = await parseUnifiedErrorResponse(res);
    throw new ApiClientError(errPayload);
  }

  const text = await res.text();
  if (!text) {
    return undefined as T;
  }
  return JSON.parse(text) as T;
}

export const dotnetHttpClient = {
  get: <T>(path: string, options?: RequestOptions) =>
    request<T>("GET", path, undefined, options),
  post: <T>(path: string, body?: unknown, options?: RequestOptions) =>
    request<T>("POST", path, body, options),
  put: <T>(path: string, body?: unknown, options?: RequestOptions) =>
    request<T>("PUT", path, body, options),
  delete: <T>(path: string, options?: RequestOptions) =>
    request<T>("DELETE", path, undefined, options),
};
