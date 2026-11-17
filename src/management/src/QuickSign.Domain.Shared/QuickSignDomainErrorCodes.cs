namespace QuickSign.Domain.Shared;

/// <summary>
/// Centralized SCREAMING_SNAKE_CASE domain error code constants across the platform.
/// Strictly synchronized with docs/error_codes.md per AGENT.md and ADR 0001.
/// </summary>
public static class QuickSignDomainErrorCodes
{
    // Authentication & Authorization
    public const string Unauthorized = "UNAUTHORIZED";
    public const string ForbiddenAccess = "FORBIDDEN_ACCESS";
    public const string InvalidCredentials = "INVALID_CREDENTIALS";
    public const string SessionExpired = "SESSION_EXPIRED";
    public const string RefreshTokenExpired = "REFRESH_TOKEN_EXPIRED";
    public const string RegistrationFailed = "REGISTRATION_FAILED";
    public const string ApiKeyInvalid = "API_KEY_INVALID";
    public const string ApiKeyRevoked = "API_KEY_REVOKED";
    public const string ApiKeyExpired = "API_KEY_EXPIRED";
    public const string ApiKeyScopeInsufficient = "API_KEY_SCOPE_INSUFFICIENT";
    public const string ApiKeyNotFound = "API_KEY_NOT_FOUND";

    // Certificates & Key Management
    public const string CertNotFound = "CERT_NOT_FOUND";
    public const string MaxActiveCertsReached = "MAX_ACTIVE_CERTS_REACHED";
    public const string CertAlreadyRevoked = "CERT_ALREADY_REVOKED";
    public const string InvalidRevocationTimestamp = "INVALID_REVOCATION_TIMESTAMP";
    public const string UnauthorizedCertificateAccess = "UNAUTHORIZED_CERTIFICATE_ACCESS";
    public const string CertKeygenFailed = "CERT_KEYGEN_FAILED";
    public const string CertRevokedAtSigning = "CERT_REVOKED_AT_SIGNING";
    public const string CertExpiredAtSigning = "CERT_EXPIRED_AT_SIGNING";
    public const string CertInactive = "CERT_INACTIVE";

    // Signing & Verification
    public const string InvalidPdfFormat = "INVALID_PDF_FORMAT";
    public const string InvalidJsonPayload = "INVALID_JSON_PAYLOAD";
    public const string SigningFailed = "SIGNING_FAILED";
    public const string TamperedContent = "TAMPERED_CONTENT";
    public const string UnknownSignature = "UNKNOWN_SIGNATURE";
    public const string CodeExistsUnverifiedContent = "CODE_EXISTS_UNVERIFIED_CONTENT";
    public const string DocumentNotFound = "DOCUMENT_NOT_FOUND";
    public const string DocumentCodeExists = "DOCUMENT_CODE_EXISTS";
    public const string InvalidPayload = "INVALID_PAYLOAD";

    // System & Infrastructure
    public const string RateLimitExceeded = "RATE_LIMIT_EXCEEDED";
    public const string InternalServerError = "INTERNAL_SERVER_ERROR";
    public const string ServiceUnavailable = "SERVICE_UNAVAILABLE";
    public const string VaultCommunicationError = "VAULT_COMMUNICATION_ERROR";
    public const string RedisCacheError = "REDIS_CACHE_ERROR";
    public const string DatabaseError = "DATABASE_ERROR";
}
