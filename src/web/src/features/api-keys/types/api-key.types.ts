export interface ApiKeyItemDto {
  id: string;
  name: string;
  keyPrefix: string;
  scopes: string;
  isActive: boolean;
  lastUsedAt?: string | null;
  expiresAt?: string | null;
  createdAt: string;
}

export interface CreateApiKeyRequestDto {
  name: string;
  scopes: string;
  expiresAt?: string | null;
}

export interface ApiKeyCreatedResponseDto {
  id: string;
  name: string;
  keyPrefix: string;
  plaintextApiKey: string;
  scopes: string;
  expiresAt?: string | null;
  createdAt: string;
}
