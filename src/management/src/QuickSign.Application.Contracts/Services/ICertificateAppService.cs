using QuickSign.Application.Contracts.DTOs.Certificates;

namespace QuickSign.Application.Contracts.Services;

public interface ICertificateAppService
{
    Task<CertificateResponseDto> IssueCertificateAsync(IssueCertificateRequestDto request, CancellationToken cancellationToken = default);
    Task RevokeCertificateAsync(Guid certificateId, RevokeCertificateRequestDto request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CertificateResponseDto>> GetMyCertificatesAsync(CancellationToken cancellationToken = default);
    Task<CertificateResponseDto> GetByIdAsync(Guid certificateId, CancellationToken cancellationToken = default);
}
