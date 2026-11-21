import { goCryptoHttpClient } from "@/services/http/goCryptoHttpClient";
import type { VerifyResult } from "@/features/verification/types/verification.types";

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
};
