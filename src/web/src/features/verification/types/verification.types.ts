export type ValidationStatus =
  | "VALID"
  | "TAMPERED_CONTENT"
  | "CERT_REVOKED"
  | "CERT_EXPIRED_AT_SIGNING"
  | "UNKNOWN_SIGNATURE"
  | "CODE_EXISTS_UNVERIFIED_CONTENT";

export interface VerifyResult {
  isValid: boolean;
  validationStatus: ValidationStatus;
  documentCode?: string;
  documentName?: string;
  signerSubject?: string;
  signingTime?: string;
  sha256Hash?: string;
  revocationChecked: boolean;
  verificationWarning?: string;
}
