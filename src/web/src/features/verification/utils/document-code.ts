const DOCUMENT_CODE_PATTERN = /^DOC-\d{4}-[0-9A-F]{8}$/;

export function normalizeDocumentCode(raw: string): string {
  return raw.trim().toUpperCase();
}

export function isValidDocumentCode(code: string): boolean {
  return DOCUMENT_CODE_PATTERN.test(normalizeDocumentCode(code));
}
