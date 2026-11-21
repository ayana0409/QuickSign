import { describe, it, expect, vi, afterEach } from "vitest";
import { goCryptoHttpClient } from "@/services/http/goCryptoHttpClient";
import { ApiClientError } from "@/services/http/api-error";

describe("goCryptoHttpClient", () => {
  const originalFetch = globalThis.fetch;

  afterEach(() => {
    globalThis.fetch = originalFetch;
    vi.restoreAllMocks();
  });

  it("sends anonymous requests with credentials omitted and no Authorization header", async () => {
    let capturedInit: RequestInit | undefined;

    globalThis.fetch = vi.fn(async (_input: RequestInfo | URL, init?: RequestInit) => {
      capturedInit = init;
      return new Response(JSON.stringify({ isValid: true, validationStatus: "VALID" }), {
        status: 200,
        headers: { "Content-Type": "application/json" },
      });
    }) as typeof fetch;

    await goCryptoHttpClient.getPublic("/verify/code/DOC-2026-6AB19FD9");

    expect(capturedInit?.credentials).toBe("omit");
    expect((capturedInit?.headers as Record<string, string>)?.Authorization).toBeUndefined();
  });

  it("maps network failures to NETWORK_ERROR", async () => {
    globalThis.fetch = vi.fn(async () => {
      throw new TypeError("Failed to fetch");
    }) as typeof fetch;

    await expect(
      goCryptoHttpClient.getPublic("/verify/code/DOC-2026-6AB19FD9")
    ).rejects.toMatchObject({
      payload: { errorCode: "NETWORK_ERROR", httpStatus: 0 },
    } satisfies Partial<ApiClientError>);
  });
});
