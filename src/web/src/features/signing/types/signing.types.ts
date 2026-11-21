export interface SignPdfInput {
  file: File;
  certificateId?: string;
  reason?: string;
  location?: string;
}

export interface SignPdfResult {
  documentCode: string;
  fileName: string;
  blob: Blob;
}

export interface SignJsonInput {
  payload: Record<string, unknown>;
  certificateId?: string;
}

export interface SignJsonResponse {
  jws: string;
  documentCode: string;
  payloadHash: string;
  createdAt: string;
}
