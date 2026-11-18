using QuickSign.Domain.Shared.Documents;

namespace QuickSign.Application.Contracts.DTOs.Documents;

public sealed class SignedDocumentDto
{
    public Guid Id { get; set; }
    public string DocumentCode { get; set; } = default!;
    public string UserId { get; set; } = default!;
    public Guid CertificateId { get; set; }
    public Guid? ApiKeyId { get; set; }
    public DocumentType DocType { get; set; }
    public string DocumentName { get; set; } = default!;
    public string OriginalHash { get; set; } = default!;
    public string SignedHash { get; set; } = default!;
    public string? StoragePath { get; set; }
    public int TotalVerifyCount { get; set; }
    public int UniqueVerifyCount { get; set; }
    public int TamperedAlertCount { get; set; }
    public DateTimeOffset? LastVerifiedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
