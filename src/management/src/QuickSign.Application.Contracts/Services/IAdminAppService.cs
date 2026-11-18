using QuickSign.Application.Contracts.DTOs.Certificates;
using QuickSign.Application.Contracts.DTOs.Documents;

namespace QuickSign.Application.Contracts.Services;

public interface IAdminAppService
{
    Task<DashboardOverviewStatsDto> GetSystemOverviewStatsAsync(CancellationToken cancellationToken = default);
    Task RevokeAnyCertificateAsync(Guid certificateId, RevokeCertificateRequestDto request, CancellationToken cancellationToken = default);
}
