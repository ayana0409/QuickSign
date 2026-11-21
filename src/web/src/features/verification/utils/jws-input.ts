const BASE64URL_SEGMENT = /^[A-Za-z0-9_-]+$/;

/**
 * Extracts a compact JWS string from raw text or a JSON wrapper ({ "jws": "..." }).
 * Returns null if the input does not resemble a 3-part JWS compact token.
 */
export function extractJwsToken(rawInput: string): string | null {
  const trimmed = rawInput.trim();
  if (!trimmed) {
    return null;
  }

  let candidate = trimmed;

  if (trimmed.startsWith("{")) {
    try {
      const parsed = JSON.parse(trimmed) as { jws?: unknown };
      if (typeof parsed.jws === "string" && parsed.jws.trim() !== "") {
        candidate = parsed.jws.trim();
      } else {
        return null;
      }
    } catch {
      return null;
    }
  }

  const parts = candidate.split(".");
  if (parts.length !== 3) {
    return null;
  }

  const [header, payload, signature] = parts;
  if (
    !BASE64URL_SEGMENT.test(header) ||
    !BASE64URL_SEGMENT.test(payload) ||
    !BASE64URL_SEGMENT.test(signature)
  ) {
    return null;
  }

  return candidate;
}
