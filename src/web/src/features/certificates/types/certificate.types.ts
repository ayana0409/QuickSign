export type KeyAlgorithmType = 1 | 2; // 1 = Rsa2048, 2 = EcdsaP256
export type CertificateStatusType = 1 | 2 | 3; // 1 = Active, 2 = Expired, 3 = Revoked

export interface CertificateResponseDto {
  id: string;
  userId: string;
  subjectName: string;
  serialNumber: string;
  algorithm: KeyAlgorithmType;
  publicKeyPem: string;
  vaultKeyPath: string;
  status: CertificateStatusType;
  revokedAt?: string | null;
  invalidatedFrom?: string | null;
  revokedReason?: string | null;
  expiresAt: string;
  createdAt: string;
}

export interface IssueCertificateRequestDto {
  subjectName: string;
  algorithm: KeyAlgorithmType;
  validityDays: number;
}

export interface RevokeCertificateRequestDto {
  invalidatedFrom?: string | null;
  reason?: string | null;
}
