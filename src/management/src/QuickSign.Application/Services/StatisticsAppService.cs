using QuickSign.Application.Contracts.DTOs.Documents;
using QuickSign.Application.Contracts.Infrastructure;
using QuickSign.Application.Contracts.Services;
using QuickSign.Domain.Repositories;

namespace QuickSign.Application.Services;

/// <summary>
/// Application service computing aggregated dashboard metrics and analytics.
/// </summary>
public sealed class StatisticsAppService : IStatisticsAppService
{
    private readonly ISignedDocumentRepository _signedDocumentRepository;
    private readonly ICertificateRepository _certificateRepository;
    private readonly IApiKeyRepository _apiKeyRepository;
    private readonly ICurrentUserService _currentUserService;

    public StatisticsAppService(
        ISignedDocumentRepository signedDocumentRepository,
        ICertificateRepository certificateRepository,
        IApiKeyRepository apiKeyRepository,
        ICurrentUserService currentUserService)
    {
        _signedDocumentRepository = signedDocumentRepository;
        _certificateRepository = certificateRepository;
        _apiKeyRepository = apiKeyRepository;
        _currentUserService = currentUserService;
    }

    public async Task<DashboardOverviewStatsDto> GetMyOverviewStatsAsync(CancellationToken cancellationToken = default)
    {
        var userId = _currentUserService.GetRequiredUserId();

        var documents = await _signedDocumentRepository.GetListByUserIdAsync(userId, 0, int.MaxValue, cancellationToken);
        var activeCertsCount = await _certificateRepository.CountActiveByUserIdAsync(userId, cancellationToken);
        var apiKeys = await _apiKeyRepository.GetListByUserIdAsync(userId, cancellationToken);

        return new DashboardOverviewStatsDto
        {
            TotalSignedDocuments = documents.Count,
            TotalVerifications = documents.Sum(d => d.TotalVerifyCount),
            UniqueVerifications = documents.Sum(d => d.UniqueVerifyCount),
            TamperedAlerts = documents.Sum(d => d.TamperedAlertCount),
            ActiveCertificates = activeCertsCount,
            ActiveApiKeys = apiKeys.Count(k => k.IsActive)
        };
    }
}
