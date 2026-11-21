import { describe, it, expect } from "vitest";
import viErrors from "@/locales/vi/errors.json";
import enErrors from "@/locales/en/errors.json";
import { ApiClientError } from "@/services/http/dotnetHttpClient";
import { mapAuthErrorMessage } from "@/features/auth/utils/auth-error-mapper";

describe("mapAuthErrorMessage", () => {
  it("maps known SCREAMING_SNAKE_CASE error codes in vi and en", () => {
    const err = new ApiClientError({
      errorCode: "INVALID_CREDENTIALS",
      httpStatus: 401,
    });

    expect(mapAuthErrorMessage(err, viErrors)).toBe(viErrors.INVALID_CREDENTIALS);
    expect(mapAuthErrorMessage(err, enErrors)).toBe(enErrors.INVALID_CREDENTIALS);
  });

  it("falls back gracefully to UNKNOWN_ERROR without exposing raw errorCode", () => {
    const err = new ApiClientError({
      errorCode: "SOME_UNMAPPED_INTERNAL_CODE",
      httpStatus: 500,
    });

    const viMsg = mapAuthErrorMessage(err, viErrors);
    expect(viMsg).toBe(viErrors.UNKNOWN_ERROR);
    expect(viMsg).not.toContain("SOME_UNMAPPED_INTERNAL_CODE");
  });

  it("handles non-ApiClientError exceptions safely", () => {
    expect(mapAuthErrorMessage(new Error("raw stack"), enErrors)).toBe(
      enErrors.UNKNOWN_ERROR
    );
  });
});
