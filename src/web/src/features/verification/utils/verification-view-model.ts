import type {
  ValidationStatus,
  VerifyResult,
} from "@/features/verification/types/verification.types";
import { normalizeDocumentCode } from "@/features/verification/utils/document-code";

export type ResultTone = "valid" | "warning" | "danger" | "neutral";

export interface VerificationViewModel {
  tone: ResultTone;
  statusKey: ValidationStatus;
}

export function getVerificationViewModel(
  status: ValidationStatus
): VerificationViewModel {
  switch (status) {
    case "VALID":
      return { tone: "valid", statusKey: "VALID" };
    case "CODE_EXISTS_UNVERIFIED_CONTENT":
      return { tone: "warning", statusKey: "CODE_EXISTS_UNVERIFIED_CONTENT" };
    case "TAMPERED_CONTENT":
      return { tone: "danger", statusKey: "TAMPERED_CONTENT" };
    case "CERT_REVOKED":
      return { tone: "danger", statusKey: "CERT_REVOKED" };
    case "CERT_EXPIRED_AT_SIGNING":
      return { tone: "warning", statusKey: "CERT_EXPIRED_AT_SIGNING" };
    case "UNKNOWN_SIGNATURE":
    default:
      return { tone: "neutral", statusKey: "UNKNOWN_SIGNATURE" };
  }
}

/**
 * Detects when a user on a QR lookup page (/verify/[documentCode]) uploads
 * a file belonging to a different document code.
 */
export function detectCodeMismatch(
  expectedCode: string | undefined,
  result: VerifyResult
): boolean {
  if (!expectedCode || !result.documentCode) {
    return false;
  }
  return (
    normalizeDocumentCode(expectedCode) !==
    normalizeDocumentCode(result.documentCode)
  );
}
