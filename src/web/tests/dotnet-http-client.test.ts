import { describe, it, expect, vi, beforeEach, afterEach } from "vitest";
import { tokenMemory } from "@/lib/token-memory";
import { dotnetHttpClient } from "@/services/http/dotnetHttpClient";

describe("dotnetHttpClient Single-Flight Refresh", () => {
  const originalFetch = globalThis.fetch;

  beforeEach(() => {
    tokenMemory.clear();
  });

  afterEach(() => {
    globalThis.fetch = originalFetch;
    vi.restoreAllMocks();
  });

  it("executes only one /api/auth/refresh call for concurrent 401 responses", async () => {
    tokenMemory.setAccessToken("expired-token");

    let refreshCallCount = 0;
    globalThis.fetch = vi.fn(async (input: RequestInfo | URL, init?: RequestInit) => {
      const url = String(input);

      if (url === "/api/auth/refresh") {
        refreshCallCount += 1;
        return new Response(
          JSON.stringify({ access_token: "fresh-token" }),
          { status: 200, headers: { "Content-Type": "application/json" } }
        );
      }

      const authHeader = (init?.headers as Record<string, string> | undefined)?.Authorization;
      if (authHeader === "Bearer expired-token") {
        return new Response(
          JSON.stringify({ errorCode: "SESSION_EXPIRED", httpStatus: 401 }),
          { status: 401, headers: { "Content-Type": "application/json" } }
        );
      }

      return new Response(
        JSON.stringify({ ok: true }),
        { status: 200, headers: { "Content-Type": "application/json" } }
      );
    }) as typeof fetch;

    const [r1, r2] = await Promise.all([
      dotnetHttpClient.get<{ ok: boolean }>("/certificates"),
      dotnetHttpClient.get<{ ok: boolean }>("/api-keys"),
    ]);

    expect(r1.ok).toBe(true);
    expect(r2.ok).toBe(true);
    expect(refreshCallCount).toBe(1);
    expect(tokenMemory.getAccessToken()).toBe("fresh-token");
  });
});
