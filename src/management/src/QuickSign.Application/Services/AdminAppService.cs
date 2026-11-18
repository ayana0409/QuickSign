using QuickSign.Application.Contracts.DTOs.Certificates;
using QuickSign.Application.Contracts.DTOs.Documents;
using QuickSign.Application.Contracts.Infrastructure;
using QuickSign.Application.Contracts.Services;
using QuickSign.Domain.Exceptions;
using QuickSign.Domain.Repositories;
using QuickSign.Domain.Shared;

namespace QuickSign.Application.Services;

/// <summary>
/// Application service handling platform administrator tasks and system-wide analytics.
/// </summary>
public sealed class AdminAppService : IAdminAppService
{
    private readonly ICertificateAppService _certificateAppService;
    private readonly ICurrentUserService _currentUserService;

    public AdminAppService(
        ICertificateAppService certificateAppService,
        ICurrentUserService currentUserService)
    {
        _certificateAppService = certificateAppService;
        _currentUserService = currentUserService;
    }

    public Task<DashboardOverviewStatsDto> GetSystemOverviewStatsAsync(CancellationToken cancellationToken = default)
    {
        EnsureAdmin();

        // System overview statistics for Admin role
        return Task.FromResult(new DashboardOverviewStatsDto());
    }

    public Task RevokeAnyCertificateAsync(
        Guid certificateId,
        RevokeCertificateRequestDto request,
        CancellationToken cancellationToken = default)
    {
        EnsureAdmin();
        return _certificateAppService.RevokeCertificateAsync(certificateId, request, cancellationToken);
    }

    private void EnsureAdmin()
    {
        if (!_currentUserService.IsInRole("Admin"))
        {
            throw new BusinessException(
                QuickSignDomainErrorCodes.ForbiddenAccess,
                "Access denied. Administrator role required.",
                403);
        }
    }
}
