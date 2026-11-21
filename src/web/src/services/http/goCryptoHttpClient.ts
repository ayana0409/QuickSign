import { tokenMemory } from "@/lib/token-memory";
import {
  ApiClientError,
  parseUnifiedErrorResponse,
} from "@/services/http/api-error";
import { refreshAccessTokenSingleFlight } from "@/services/http/dotnetHttpClient";

const GO_CRYPTO_BASE_URL =
  process.env.NEXT_PUBLIC_GO_CRYPTO_API_URL ?? "http://localhost:50052/api/v1";

const DEFAULT_TIMEOUT_MS = 30_000;

interface GoRequestOptions {
  signal?: AbortSignal;
  timeoutMs?: number;
  _isRetry?: boolean;
}

export interface BinaryDownloadResponse {
  blob: Blob;
  documentCode: string;
  fileName: string;
}

function buildRequestSignal(
  callerSignal?: AbortSignal,
  timeoutMs = DEFAULT_TIMEOUT_MS
): AbortSignal {
  const timeoutSignal = AbortSignal.timeout(timeoutMs);
  if (!callerSignal) {
    return timeoutSignal;
  }
  return AbortSignal.any([callerSignal, timeoutSignal]);
}

function parseContentDispositionFileName(header: string | null): string | null {
  if (!header) return null;
  const utfMatch = /filename\*=UTF-8''([^;]+)/i.exec(header);
  if (utfMatch?.[1]) {
    try {
      return decodeURIComponent(utfMatch[1].trim());
    } catch {
      return utfMatch[1].trim();
    }
  }
  const quotedMatch = /filename="([^"]+)"/i.exec(header);
  if (quotedMatch?.[1]) {
    return quotedMatch[1].trim();
  }
  const plainMatch = /filename=([^;]+)/i.exec(header);
  return plainMatch?.[1]?.trim() ?? null;
}

async function executeFetch(
  method: "GET" | "POST",
  path: string,
  body: FormData | string | undefined,
  headers: Record<string, string>,
  credentials: RequestCredentials,
  options: GoRequestOptions
): Promise<Response> {
  try {
    return await fetch(`${GO_CRYPTO_BASE_URL}${path}`, {
      method,
      headers,
      body,
      credentials,
      cache: "no-store",
      signal: buildRequestSignal(options.signal, options.timeoutMs),
    });
  } catch (err) {
    if (err instanceof DOMException && err.name === "AbortError") {
      throw err;
    }
    if (err instanceof DOMException && err.name === "TimeoutError") {
      throw new ApiClientError({
        errorCode: "SERVICE_UNAVAILABLE",
        httpStatus: 503,
      });
    }
    throw new ApiClientError({
      errorCode: "NETWORK_ERROR",
      httpStatus: 0,
    });
  }
}

async function requestPublic<T>(
  method: "GET" | "POST",
  path: string,
  body?: FormData | string,
  contentType?: string,
  options: GoRequestOptions = {}
): Promise<T> {
  const headers: Record<string, string> = {};
  if (contentType) {
    headers["Content-Type"] = contentType;
  }

  const res = await executeFetch(method, path, body, headers, "omit", options);
  if (!res.ok) {
    const errPayload = await parseUnifiedErrorResponse(res);
    throw new ApiClientError(errPayload);
  }

  return (await res.json()) as T;
}

async function requestAuthenticated(
  method: "POST",
  path: string,
  body: FormData | string,
  contentType: string | undefined,
  options: GoRequestOptions = {}
): Promise<Response> {
  const headers: Record<string, string> = {};
  if (contentType) {
    headers["Content-Type"] = contentType;
  }

  const token = tokenMemory.getAccessToken();
  if (token) {
    headers.Authorization = `Bearer ${token}`;
  }

  const res = await executeFetch(method, path, body, headers, "omit", options);

  if (res.status === 401 && !options._isRetry) {
    const newToken = await refreshAccessTokenSingleFlight();
    if (newToken) {
      return requestAuthenticated(method, path, body, contentType, {
        ...options,
        _isRetry: true,
      });
    }
  }

  if (!res.ok) {
    const errPayload = await parseUnifiedErrorResponse(res);
    throw new ApiClientError(errPayload);
  }

  return res;
}

export const goCryptoHttpClient = {
  getPublic: <T>(path: string, options?: GoRequestOptions) =>
    requestPublic<T>("GET", path, undefined, undefined, options),

  postPublicJson: <T>(path: string, body: unknown, options?: GoRequestOptions) =>
    requestPublic<T>(
      "POST",
      path,
      JSON.stringify(body),
      "application/json",
      options
    ),

  postPublicFormData: <T>(
    path: string,
    formData: FormData,
    options?: GoRequestOptions
  ) => requestPublic<T>("POST", path, formData, undefined, options),

  postAuthenticatedJson: async <T>(
    path: string,
    body: unknown,
    options?: GoRequestOptions
  ): Promise<T> => {
    const res = await requestAuthenticated(
      "POST",
      path,
      JSON.stringify(body),
      "application/json",
      options
    );
    return (await res.json()) as T;
  },

  postAuthenticatedBlob: async (
    path: string,
    formData: FormData,
    fallbackFileName: string,
    options?: GoRequestOptions
  ): Promise<BinaryDownloadResponse> => {
    const res = await requestAuthenticated(
      "POST",
      path,
      formData,
      undefined,
      options
    );
    const blob = await res.blob();
    const documentCode = res.headers.get("X-Document-Code") ?? "";
    const fileName =
      parseContentDispositionFileName(res.headers.get("Content-Disposition")) ??
      fallbackFileName;

    return { blob, documentCode, fileName };
  },
};
