using QuickSign.Domain.SeedWork;
using QuickSign.Domain.Shared.Documents;

namespace QuickSign.Domain.Documents;

/// <summary>
/// SignedDocument Aggregate Root recording digitally signed files, cryptographic hashes, and verification metrics.
/// </summary>
public sealed class SignedDocument : AggregateRoot<Guid>
{
    public DocumentCode DocumentCode { get; private set; } = default!;
    public string UserId { get; private set; } = default!;
    public Guid CertificateId { get; private set; }
    public Guid? ApiKeyId { get; private set; }
    public DocumentType DocType { get; private set; }
    public string DocumentName { get; private set; } = default!;
    public Sha256Hash OriginalHash { get; private set; } = default!;
    public Sha256Hash SignedHash { get; private set; } = default!;
    public string SignatureValue { get; private set; } = default!;
    public string? StoragePath { get; private set; }
    public int TotalVerifyCount { get; private set; }
    public int UniqueVerifyCount { get; private set; }
    public int TamperedAlertCount { get; private set; }
    public DateTimeOffset? LastVerifiedAt { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    private readonly List<VerificationLog> _verificationLogs = new();
    public IReadOnlyCollection<VerificationLog> VerificationLogs => _verificationLogs.AsReadOnly();

    private SignedDocument() { }

    public SignedDocument(
        Guid id,
        DocumentCode documentCode,
        string userId,
        Guid certificateId,
        Guid? apiKeyId,
        DocumentType docType,
        string documentName,
        Sha256Hash originalHash,
        Sha256Hash signedHash,
        string signatureValue,
        string? storagePath = null) : base(id)
    {
        if (string.IsNullOrWhiteSpace(userId))
            throw new ArgumentException("UserId cannot be empty.", nameof(userId));

        if (string.IsNullOrWhiteSpace(documentName))
            throw new ArgumentException("DocumentName cannot be empty.", nameof(documentName));

        if (string.IsNullOrWhiteSpace(signatureValue))
            throw new ArgumentException("SignatureValue cannot be empty.", nameof(signatureValue));

        DocumentCode = documentCode ?? throw new ArgumentNullException(nameof(documentCode));
        UserId = userId.Trim();
        CertificateId = certificateId;
        ApiKeyId = apiKeyId;
        DocType = docType;
        DocumentName = documentName.Trim();
        OriginalHash = originalHash ?? throw new ArgumentNullException(nameof(originalHash));
        SignedHash = signedHash ?? throw new ArgumentNullException(nameof(signedHash));
        SignatureValue = signatureValue.Trim();
        StoragePath = storagePath?.Trim();
        TotalVerifyCount = 0;
        UniqueVerifyCount = 0;
        TamperedAlertCount = 0;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Increments document verification counters atomically.
    /// </summary>
    public void IncrementMetrics(bool isUnique, bool isTampered, DateTimeOffset timestamp)
    {
        TotalVerifyCount++;
        if (isUnique) UniqueVerifyCount++;
        if (isTampered) TamperedAlertCount++;
        LastVerifiedAt = timestamp;
    }
}
