export interface UnifiedApiError {
  errorCode: string;
  httpStatus: number;
  timestamp?: string;
  traceId?: string;
  message?: string;
  details?: Record<string, unknown>;
}

export class ApiClientError extends Error {
  readonly payload: UnifiedApiError;

  constructor(payload: UnifiedApiError) {
    super(payload.errorCode);
    this.name = "ApiClientError";
    this.payload = payload;
  }
}

export async function parseUnifiedErrorResponse(res: Response): Promise<UnifiedApiError> {
  try {
    const json = (await res.json()) as Partial<UnifiedApiError>;
    return {
      errorCode: json.errorCode || "UNKNOWN_ERROR",
      httpStatus: json.httpStatus || res.status,
      timestamp: json.timestamp,
      traceId: json.traceId,
      message: json.message,
      details: json.details,
    };
  } catch {
    return {
      errorCode: "UNKNOWN_ERROR",
      httpStatus: res.status,
    };
  }
}
