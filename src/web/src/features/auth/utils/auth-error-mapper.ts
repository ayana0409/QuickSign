import { ApiClientError } from "@/services/http/dotnetHttpClient";
import type { TranslationDictionary } from "@/i18n/types";

/**
 * Maps backend SCREAMING_SNAKE_CASE error codes (or network failures) into
 * localized, human-friendly messages from the active i18n dictionary.
 * Never exposes raw error codes or stack traces to end users.
 */
export function mapAuthErrorMessage(
  err: unknown,
  errorsDict: TranslationDictionary["errors"]
): string {
  if (err instanceof ApiClientError) {
    const code = err.payload.errorCode as keyof TranslationDictionary["errors"];
    if (code && code in errorsDict) {
      return errorsDict[code];
    }
    return errorsDict.UNKNOWN_ERROR;
  }

  return errorsDict.UNKNOWN_ERROR;
}
