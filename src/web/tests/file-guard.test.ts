import { describe, it, expect } from "vitest";
import {
  MAX_VERIFY_FILE_BYTES,
  validatePdfFile,
} from "@/features/verification/utils/file-guard";

describe("validatePdfFile", () => {
  it("accepts valid PDF files starting with %PDF-", async () => {
    const file = new File(["%PDF-1.7\ncontent"], "contract.pdf", {
      type: "application/pdf",
    });
    expect(await validatePdfFile(file)).toBeNull();
  });

  it("rejects empty files", async () => {
    const file = new File([], "empty.pdf", { type: "application/pdf" });
    expect(await validatePdfFile(file)).toBe("FILE_EMPTY");
  });

  it("rejects files larger than 20 MB", async () => {
    const oversized = new Uint8Array(MAX_VERIFY_FILE_BYTES + 1);
    oversized.set([0x25, 0x50, 0x44, 0x46, 0x2d]);
    const file = new File([oversized], "huge.pdf", { type: "application/pdf" });
    expect(await validatePdfFile(file)).toBe("FILE_TOO_LARGE");
  });

  it("rejects non-PDF files renamed to .pdf", async () => {
    const file = new File(["<html>not a pdf</html>"], "fake.pdf", {
      type: "application/pdf",
    });
    expect(await validatePdfFile(file)).toBe("NOT_A_PDF");
  });
});
