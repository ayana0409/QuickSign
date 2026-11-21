import { describe, it, expect, vi, afterEach, beforeEach } from "vitest";
import { tokenMemory } from "@/lib/token-memory";
import { goCryptoHttpClient } from "@/services/http/goCryptoHttpClient";
import { ApiClientError } from "@/services/http/api-error";

describe("goCryptoHttpClient", () => {
  const originalFetch = globalThis.fetch;

  beforeEach(() => {
    tokenMemory.clear();
  });

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

  it("refreshes token on 401 and parses X-Document-Code + Content-Disposition for signed PDF", async () => {
    tokenMemory.setAccessToken("expired-jwt");

    globalThis.fetch = vi.fn(async (input: RequestInfo | URL, init?: RequestInit) => {
      const url = String(input);
      if (url === "/api/auth/refresh") {
        return new Response(JSON.stringify({ access_token: "fresh-jwt" }), {
          status: 200,
          headers: { "Content-Type": "application/json" },
        });
      }

      const authHeader = (init?.headers as Record<string, string> | undefined)?.Authorization;
      if (authHeader === "Bearer expired-jwt") {
        return new Response(
          JSON.stringify({ errorCode: "SESSION_EXPIRED", httpStatus: 401 }),
          { status: 401, headers: { "Content-Type": "application/json" } }
        );
      }

      return new Response(new Uint8Array([0x25, 0x50, 0x44, 0x46, 0x2d]), {
        status: 200,
        headers: {
          "Content-Type": "application/pdf",
          "X-Document-Code": "DOC-2026-AABB1122",
          "Content-Disposition": 'attachment; filename="contract_signed.pdf"',
        },
      });
    }) as typeof fetch;

    const formData = new FormData();
    const res = await goCryptoHttpClient.postAuthenticatedBlob(
      "/sign/pdf",
      formData,
      "fallback.pdf"
    );

    expect(res.documentCode).toBe("DOC-2026-AABB1122");
    expect(res.fileName).toBe("contract_signed.pdf");
    expect(tokenMemory.getAccessToken()).toBe("fresh-jwt");
  });
});
