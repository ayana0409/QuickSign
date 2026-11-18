using QuickSign.Application.Contracts.DTOs.Documents;

namespace QuickSign.Application.Contracts.Services;

public interface IStatisticsAppService
{
    Task<DashboardOverviewStatsDto> GetMyOverviewStatsAsync(CancellationToken cancellationToken = default);
}
