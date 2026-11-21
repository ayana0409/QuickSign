import {
  ApiClientError,
  parseUnifiedErrorResponse,
} from "@/services/http/api-error";

const GO_CRYPTO_BASE_URL =
  process.env.NEXT_PUBLIC_GO_CRYPTO_API_URL ?? "http://localhost:50052/api/v1";

const DEFAULT_TIMEOUT_MS = 30_000;

interface GoRequestOptions {
  signal?: AbortSignal;
  timeoutMs?: number;
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

  let res: Response;
  try {
    res = await fetch(`${GO_CRYPTO_BASE_URL}${path}`, {
      method,
      headers,
      body,
      // Public verification endpoints must remain anonymous so Go does not
      // receive unrelated browser cookies or auth headers.
      credentials: "omit",
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

  if (!res.ok) {
    const errPayload = await parseUnifiedErrorResponse(res);
    throw new ApiClientError(errPayload);
  }

  return (await res.json()) as T;
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
};
