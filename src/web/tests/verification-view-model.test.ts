import { describe, it, expect } from "vitest";
import {
  detectCodeMismatch,
  getVerificationViewModel,
} from "@/features/verification/utils/verification-view-model";

describe("verification-view-model", () => {
  it("maps statuses to expected visual tones", () => {
    expect(getVerificationViewModel("VALID").tone).toBe("valid");
    expect(getVerificationViewModel("CODE_EXISTS_UNVERIFIED_CONTENT").tone).toBe("warning");
    expect(getVerificationViewModel("TAMPERED_CONTENT").tone).toBe("danger");
    expect(getVerificationViewModel("CERT_REVOKED").tone).toBe("danger");
    expect(getVerificationViewModel("UNKNOWN_SIGNATURE").tone).toBe("neutral");
  });

  it("detects mismatch when uploaded file belongs to another code", () => {
    expect(
      detectCodeMismatch("DOC-2026-1111AAAA", {
        isValid: true,
        validationStatus: "VALID",
        documentCode: "DOC-2026-2222BBBB",
        revocationChecked: true,
      })
    ).toBe(true);

    expect(
      detectCodeMismatch("doc-2026-1111aaaa", {
        isValid: true,
        validationStatus: "VALID",
        documentCode: "DOC-2026-1111AAAA",
        revocationChecked: true,
      })
    ).toBe(false);
  });
});
