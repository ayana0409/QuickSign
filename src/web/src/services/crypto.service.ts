import { goCryptoHttpClient } from "@/services/http/goCryptoHttpClient";
import type { VerifyResult } from "@/features/verification/types/verification.types";
import type {
  SignJsonInput,
  SignJsonResponse,
  SignPdfInput,
  SignPdfResult,
} from "@/features/signing/types/signing.types";

export const cryptoService = {
  verifyPdfFile(file: File, signal?: AbortSignal): Promise<VerifyResult> {
    const formData = new FormData();
    formData.append("file", file);
    return goCryptoHttpClient.postPublicFormData<VerifyResult>(
      "/verify/file",
      formData,
      { signal }
    );
  },

  verifyJwsToken(jws: string, signal?: AbortSignal): Promise<VerifyResult> {
    return goCryptoHttpClient.postPublicJson<VerifyResult>(
      "/verify/file",
      { jws },
      { signal }
    );
  },

  lookupDocumentCode(
    documentCode: string,
    signal?: AbortSignal
  ): Promise<VerifyResult> {
    return goCryptoHttpClient.getPublic<VerifyResult>(
      `/verify/code/${encodeURIComponent(documentCode)}`,
      { signal }
    );
  },

  signPdf(input: SignPdfInput, signal?: AbortSignal): Promise<SignPdfResult> {
    const formData = new FormData();
    formData.append("file", input.file);
    if (input.certificateId) {
      formData.append("certificateId", input.certificateId);
    }
    if (input.reason) {
      formData.append("reason", input.reason);
    }
    if (input.location) {
      formData.append("location", input.location);
    }

    const baseName = input.file.name.replace(/\.pdf$/i, "") || "document";
    return goCryptoHttpClient.postAuthenticatedBlob(
      "/sign/pdf",
      formData,
      `${baseName}_signed.pdf`,
      { signal }
    );
  },

  signJson(input: SignJsonInput, signal?: AbortSignal): Promise<SignJsonResponse> {
    return goCryptoHttpClient.postAuthenticatedJson<SignJsonResponse>(
      "/sign/json",
      input,
      { signal }
    );
  },
};
