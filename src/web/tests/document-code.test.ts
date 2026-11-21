import { describe, it, expect } from "vitest";
import {
  isValidDocumentCode,
  normalizeDocumentCode,
} from "@/features/verification/utils/document-code";

describe("document-code utilities", () => {
  it("normalizes whitespace and lowercase letters", () => {
    expect(normalizeDocumentCode("  doc-2026-6ab19fd9 ")).toBe("DOC-2026-6AB19FD9");
  });

  it("accepts valid DOC-YYYY-XXXXXXXX codes", () => {
    expect(isValidDocumentCode("DOC-2026-6AB19FD9")).toBe(true);
    expect(isValidDocumentCode(" doc-2026-00ff12aa ")).toBe(true);
  });

  it("rejects malformed or path-traversal codes", () => {
    expect(isValidDocumentCode("")).toBe(false);
    expect(isValidDocumentCode("DOC-2026-123")).toBe(false);
    expect(isValidDocumentCode("../etc/passwd")).toBe(false);
    expect(isValidDocumentCode("DOC-2026-ZZZZZZZZ")).toBe(false);
  });
});
