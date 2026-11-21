// Pure in-memory access token storage.
// Never persists access_token to localStorage, sessionStorage, or JS-readable cookies
// to eliminate XSS token theft vectors.

export interface DecodedJwtUser {
  userId: string;
  userName: string;
  email?: string;
  roles: string[];
  isAdmin: boolean;
  expiresAt: number;
}

let inMemoryAccessToken: string | null = null;
const listeners = new Set<(token: string | null) => void>();

export const tokenMemory = {
  getAccessToken(): string | null {
    return inMemoryAccessToken;
  },

  setAccessToken(token: string | null): void {
    inMemoryAccessToken = token;
    listeners.forEach((listener) => listener(token));
  },

  clear(): void {
    inMemoryAccessToken = null;
    listeners.forEach((listener) => listener(null));
  },

  subscribe(listener: (token: string | null) => void): () => void {
    listeners.add(listener);
    return () => {
      listeners.delete(listener);
    };
  },
};

/**
 * Decodes a Base64Url string into a UTF-8 string safely in both browser and Node runtimes.
 */
function decodeBase64Url(input: string): string {
  const normalized = input.replace(/-/g, "+").replace(/_/g, "/");
  const padded = normalized.padEnd(normalized.length + ((4 - (normalized.length % 4)) % 4), "=");

  if (typeof window !== "undefined" && typeof window.atob === "function") {
    const binary = window.atob(padded);
    const bytes = Uint8Array.from(binary, (char) => char.charCodeAt(0));
    return new TextDecoder().decode(bytes);
  }

  return Buffer.from(padded, "base64").toString("utf-8");
}

/**
 * Extracts user identity and RBAC roles from a JWT access token without trusting it
 * for authorization decisions (backend always verifies cryptographic signature).
 * Handles both single-string and array `role` claims emitted by QuickBite Identity.
 */
export function decodeJwtUser(token: string | null): DecodedJwtUser | null {
  if (!token) {
    return null;
  }

  const parts = token.split(".");
  if (parts.length !== 3) {
    return null;
  }

  try {
    const payloadJson = decodeBase64Url(parts[1]);
    const claims = JSON.parse(payloadJson) as Record<string, unknown>;

    const userId =
      (claims.sub as string | undefined) ??
      (claims["http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier"] as string | undefined) ??
      "";

    if (!userId) {
      return null;
    }

    const userName =
      (claims.preferred_username as string | undefined) ??
      (claims.unique_name as string | undefined) ??
      (claims.name as string | undefined) ??
      userId;

    const email = claims.email as string | undefined;

    const rawRole =
      claims.role ??
      claims["http://schemas.microsoft.com/ws/2008/06/identity/claims/role"];

    let roles: string[] = [];
    if (Array.isArray(rawRole)) {
      roles = rawRole.filter((r): r is string => typeof r === "string");
    } else if (typeof rawRole === "string" && rawRole.trim() !== "") {
      roles = [rawRole.trim()];
    }

    const isAdmin = roles.some((r) => {
      const lower = r.toLowerCase();
      return lower === "admin" || lower === "sso-admin";
    });

    const expiresAt = typeof claims.exp === "number" ? claims.exp : 0;

    return {
      userId,
      userName,
      email,
      roles,
      isAdmin,
      expiresAt,
    };
  } catch {
    return null;
  }
}

export function isJwtExpired(token: string | null, skewSeconds = 30): boolean {
  const decoded = decodeJwtUser(token);
  if (!decoded || !decoded.expiresAt) {
    return true;
  }
  const nowSeconds = Math.floor(Date.now() / 1000);
  return decoded.expiresAt <= nowSeconds + skewSeconds;
}
