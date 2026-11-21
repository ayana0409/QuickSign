import { dotnetHttpClient } from "@/services/http/dotnetHttpClient";
import type {
  SignedDocumentDto,
  VerificationLogDto,
} from "@/features/documents/types/document.types";

export const documentService = {
  getMyDocuments(
    skipCount = 0,
    maxResultCount = 20
  ): Promise<SignedDocumentDto[]> {
    return dotnetHttpClient.get<SignedDocumentDto[]>(
      `/documents?skipCount=${skipCount}&maxResultCount=${maxResultCount}`
    );
  },

  getById(id: string): Promise<SignedDocumentDto> {
    return dotnetHttpClient.get<SignedDocumentDto>(
      `/documents/${encodeURIComponent(id)}`
    );
  },

  getVerificationHistory(
    id: string,
    skipCount = 0,
    maxResultCount = 50
  ): Promise<VerificationLogDto[]> {
    return dotnetHttpClient.get<VerificationLogDto[]>(
      `/documents/${encodeURIComponent(id)}/verifications?skipCount=${skipCount}&maxResultCount=${maxResultCount}`
    );
  },
};
