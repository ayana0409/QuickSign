export const MAX_VERIFY_FILE_BYTES = 20 * 1024 * 1024; // 20 MB per API spec

export type FileGuardErrorKey =
  | "FILE_EMPTY"
  | "FILE_TOO_LARGE"
  | "NOT_A_PDF";

export async function validatePdfFile(file: File): Promise<FileGuardErrorKey | null> {
  if (file.size === 0) {
    return "FILE_EMPTY";
  }

  if (file.size > MAX_VERIFY_FILE_BYTES) {
    return "FILE_TOO_LARGE";
  }

  // Inspect only the first 5 bytes (%PDF-) without loading the whole file into memory.
  const headerSlice = file.slice(0, 5);
  const buffer = await headerSlice.arrayBuffer();
  const header = new TextDecoder("ascii").decode(buffer);

  if (header !== "%PDF-") {
    return "NOT_A_PDF";
  }

  return null;
}
