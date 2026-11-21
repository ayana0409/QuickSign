import type viCommon from "@/locales/vi/common.json";
import type viAuth from "@/locales/vi/auth.json";
import type viErrors from "@/locales/vi/errors.json";
import type viVerify from "@/locales/vi/verify.json";

export type Locale = "vi" | "en";

export interface TranslationDictionary {
  common: typeof viCommon;
  auth: typeof viAuth;
  errors: typeof viErrors;
  verify: typeof viVerify;
}
