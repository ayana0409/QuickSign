import { dotnetHttpClient } from "@/services/http/dotnetHttpClient";
import type {
  ApiKeyCreatedResponseDto,
  ApiKeyItemDto,
  CreateApiKeyRequestDto,
} from "@/features/api-keys/types/api-key.types";

export const apiKeyService = {
  getMyApiKeys(): Promise<ApiKeyItemDto[]> {
    return dotnetHttpClient.get<ApiKeyItemDto[]>("/api-keys");
  },

  createApiKey(
    input: CreateApiKeyRequestDto
  ): Promise<ApiKeyCreatedResponseDto> {
    return dotnetHttpClient.post<ApiKeyCreatedResponseDto>("/api-keys", input);
  },

  revokeApiKey(id: string): Promise<void> {
    return dotnetHttpClient.delete<void>(`/api-keys/${encodeURIComponent(id)}`);
  },
};
