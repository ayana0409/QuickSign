namespace QuickSign.Application.Contracts.DTOs.Certificates;

public sealed class RevokeCertificateRequestDto
{
    public DateTimeOffset? InvalidatedFrom { get; set; }
    public string? Reason { get; set; }
}
