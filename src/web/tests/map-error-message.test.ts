import { describe, it, expect } from "vitest";
import viErrors from "@/locales/vi/errors.json";
import enErrors from "@/locales/en/errors.json";
import { ApiClientError } from "@/services/http/api-error";
import { mapErrorMessage } from "@/i18n/map-error-message";

describe("mapErrorMessage", () => {
  it("maps known SCREAMING_SNAKE_CASE error codes in vi and en", () => {
    const err = new ApiClientError({
      errorCode: "INVALID_CREDENTIALS",
      httpStatus: 401,
    });

    expect(mapErrorMessage(err, viErrors)).toBe(viErrors.INVALID_CREDENTIALS);
    expect(mapErrorMessage(err, enErrors)).toBe(enErrors.INVALID_CREDENTIALS);
  });

  it("falls back gracefully to UNKNOWN_ERROR without exposing raw errorCode", () => {
    const err = new ApiClientError({
      errorCode: "SOME_UNMAPPED_INTERNAL_CODE",
      httpStatus: 500,
    });

    const viMsg = mapErrorMessage(err, viErrors);
    expect(viMsg).toBe(viErrors.UNKNOWN_ERROR);
    expect(viMsg).not.toContain("SOME_UNMAPPED_INTERNAL_CODE");
  });

  it("handles non-ApiClientError exceptions safely", () => {
    expect(mapErrorMessage(new Error("raw stack"), enErrors)).toBe(
      enErrors.UNKNOWN_ERROR
    );
  });
});
