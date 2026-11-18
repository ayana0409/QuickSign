using QuickSign.Domain.Shared.Documents;

namespace QuickSign.Application.Contracts.DTOs.Documents;

public sealed class VerificationLogDto
{
    public long Id { get; set; }
    public Guid? SignedDocumentId { get; set; }
    public string SubmittedHash { get; set; } = default!;
    public VerificationMethodType VerificationMethod { get; set; }
    public Guid? ApiKeyId { get; set; }
    public string VerifierIp { get; set; } = default!;
    public string? UserAgent { get; set; }
    public bool IsValid { get; set; }
    public ValidationStatusType ValidationStatus { get; set; }
    public DateTimeOffset VerifiedAt { get; set; }
}
