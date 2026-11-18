using QuickSign.Domain.Shared.Certificates;

namespace QuickSign.Application.Contracts.DTOs.Certificates;

public sealed class IssueCertificateRequestDto
{
    public string SubjectName { get; set; } = default!;
    public KeyAlgorithmType Algorithm { get; set; } = KeyAlgorithmType.Rsa2048;
    public int ValidityDays { get; set; } = 365;
}
