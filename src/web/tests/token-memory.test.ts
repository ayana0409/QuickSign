import { describe, it, expect, beforeEach } from "vitest";
import { decodeJwtUser, isJwtExpired, tokenMemory } from "@/lib/token-memory";

function makeJwt(payload: Record<string, unknown>): string {
  const header = Buffer.from(JSON.stringify({ alg: "RS256", typ: "JWT" })).toString("base64url");
  const body = Buffer.from(JSON.stringify(payload)).toString("base64url");
  return `${header}.${body}.signature`;
}

describe("tokenMemory & decodeJwtUser", () => {
  beforeEach(() => {
    tokenMemory.clear();
  });

  it("stores and clears access token strictly in memory", () => {
    expect(tokenMemory.getAccessToken()).toBeNull();
    tokenMemory.setAccessToken("abc.def.ghi");
    expect(tokenMemory.getAccessToken()).toBe("abc.def.ghi");
    tokenMemory.clear();
    expect(tokenMemory.getAccessToken()).toBeNull();
  });

  it("decodes UTF-8 username and single-string role claim", () => {
    const futureExp = Math.floor(Date.now() / 1000) + 3600;
    const token = makeJwt({
      sub: "user-123",
      preferred_username: "Nguyễn Văn A",
      email: "a@quicksign.vn",
      role: "Admin",
      exp: futureExp,
    });

    const decoded = decodeJwtUser(token);
    expect(decoded).not.toBeNull();
    expect(decoded?.userId).toBe("user-123");
    expect(decoded?.userName).toBe("Nguyễn Văn A");
    expect(decoded?.email).toBe("a@quicksign.vn");
    expect(decoded?.roles).toEqual(["Admin"]);
    expect(decoded?.isAdmin).toBe(true);
    expect(isJwtExpired(token)).toBe(false);
  });

  it("decodes array role claims from QuickBite Identity", () => {
    const futureExp = Math.floor(Date.now() / 1000) + 3600;
    const token = makeJwt({
      sub: "user-456",
      role: ["User", "sso-admin"],
      exp: futureExp,
    });

    const decoded = decodeJwtUser(token);
    expect(decoded?.roles).toEqual(["User", "sso-admin"]);
    expect(decoded?.isAdmin).toBe(true);
  });

  it("detects expired JWT tokens", () => {
    const pastExp = Math.floor(Date.now() / 1000) - 60;
    const token = makeJwt({ sub: "user-789", exp: pastExp });
    expect(isJwtExpired(token)).toBe(true);
  });
});
