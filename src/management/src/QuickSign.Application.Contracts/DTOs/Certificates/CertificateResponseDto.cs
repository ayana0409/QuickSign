using QuickSign.Domain.Shared.Certificates;

namespace QuickSign.Application.Contracts.DTOs.Certificates;

public sealed class CertificateResponseDto
{
    public Guid Id { get; set; }
    public string UserId { get; set; } = default!;
    public string SubjectName { get; set; } = default!;
    public string SerialNumber { get; set; } = default!;
    public KeyAlgorithmType Algorithm { get; set; }
    public string PublicKeyPem { get; set; } = default!;
    public string VaultKeyPath { get; set; } = default!;
    public CertificateStatusType Status { get; set; }
    public DateTimeOffset? RevokedAt { get; set; }
    public DateTimeOffset? InvalidatedFrom { get; set; }
    public string? RevokedReason { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
