import { describe, it, expect } from "vitest";
import { extractJwsToken } from "@/features/verification/utils/jws-input";

describe("extractJwsToken", () => {
  it("extracts a compact 3-part JWS string", () => {
    const token = "eyJhbGciOiJSUzI1NiJ9.eyJzdWIiOiIxMjMifQ.c2lnbmF0dXJl";
    expect(extractJwsToken(`  ${token}  `)).toBe(token);
  });

  it("extracts JWS from JSON wrapper output", () => {
    const token = "eyJhbGciOiJSUzI1NiJ9.eyJzdWIiOiIxMjMifQ.c2lnbmF0dXJl";
    const json = JSON.stringify({ jws: token, documentCode: "DOC-2026-6AB19FD9" });
    expect(extractJwsToken(json)).toBe(token);
  });

  it("rejects invalid or incomplete strings", () => {
    expect(extractJwsToken("")).toBeNull();
    expect(extractJwsToken("only.two")).toBeNull();
    expect(extractJwsToken("not base64!.payload.sig")).toBeNull();
    expect(extractJwsToken("{bad json")).toBeNull();
  });
});
