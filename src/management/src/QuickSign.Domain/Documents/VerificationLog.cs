using QuickSign.Domain.SeedWork;
using QuickSign.Domain.Shared.Documents;

namespace QuickSign.Domain.Documents;

/// <summary>
/// Child Entity capturing an individual cryptographic verification event for audit and security analysis.
/// </summary>
public sealed class VerificationLog : Entity<long>
{
    public Guid? SignedDocumentId { get; private set; }
    public Sha256Hash SubmittedHash { get; private set; } = default!;
    public VerificationMethodType VerificationMethod { get; private set; }
    public Guid? ApiKeyId { get; private set; }
    public string VerifierIp { get; private set; } = default!;
    public string? UserAgent { get; private set; }
    public bool IsValid { get; private set; }
    public ValidationStatusType ValidationStatus { get; private set; }
    public DateTimeOffset VerifiedAt { get; private set; }

    private VerificationLog() { }

    public VerificationLog(
        Guid? signedDocumentId,
        Sha256Hash submittedHash,
        VerificationMethodType verificationMethod,
        Guid? apiKeyId,
        string verifierIp,
        string? userAgent,
        bool isValid,
        ValidationStatusType validationStatus,
        DateTimeOffset verifiedAt)
    {
        if (string.IsNullOrWhiteSpace(verifierIp))
            throw new ArgumentException("Verifier IP cannot be empty.", nameof(verifierIp));

        SignedDocumentId = signedDocumentId;
        SubmittedHash = submittedHash ?? throw new ArgumentNullException(nameof(submittedHash));
        VerificationMethod = verificationMethod;
        ApiKeyId = apiKeyId;
        VerifierIp = verifierIp.Trim();
        UserAgent = userAgent?.Trim();
        IsValid = isValid;
        ValidationStatus = validationStatus;
        VerifiedAt = verifiedAt;
    }
}
