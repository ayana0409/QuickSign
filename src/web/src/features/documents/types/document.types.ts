export type DocumentType = 1 | 2; // 1 = Pdf, 2 = Json
export type VerificationMethodType = 1 | 2 | 3; // 1 = WebUpload, 2 = WebCode, 3 = Api
export type ValidationStatusType = 1 | 2 | 3 | 4 | 5 | 6;

export interface SignedDocumentDto {
  id: string;
  documentCode: string;
  userId: string;
  certificateId: string;
  apiKeyId?: string | null;
  docType: DocumentType;
  documentName: string;
  originalHash: string;
  signedHash: string;
  storagePath?: string | null;
  totalVerifyCount: number;
  uniqueVerifyCount: number;
  tamperedAlertCount: number;
  lastVerifiedAt?: string | null;
  createdAt: string;
}

export interface VerificationLogDto {
  id: number;
  signedDocumentId?: string | null;
  submittedHash: string;
  verificationMethod: VerificationMethodType;
  apiKeyId?: string | null;
  verifierIp: string;
  userAgent?: string | null;
  isValid: boolean;
  validationStatus: ValidationStatusType;
  verifiedAt: string;
}
