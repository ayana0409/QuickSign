namespace QuickSign.Domain.Shared.Documents;

/// <summary>
/// Detailed result classification for cryptographic verification.
/// </summary>
public enum ValidationStatusType
{
    /// <summary>
    /// Signature is cryptographically valid and intact.
    /// </summary>
    Valid = 1,

    /// <summary>
    /// Document content has been tampered with or modified after signing.
    /// </summary>
    TamperedContent = 2,

    /// <summary>
    /// The signing certificate was revoked prior to the signing timestamp.
    /// </summary>
    CertRevoked = 3,

    /// <summary>
    /// The signing certificate had expired prior to the signing timestamp.
    /// </summary>
    CertExpiredAtSigning = 4,

    /// <summary>
    /// The document contains no recognized QuickSign digital signature blob.
    /// </summary>
    UnknownSignature = 5,

    /// <summary>
    /// QR Code lookup confirmed code exists, but binary content requires uploading to verify.
    /// </summary>
    CodeExistsUnverifiedContent = 6
}
