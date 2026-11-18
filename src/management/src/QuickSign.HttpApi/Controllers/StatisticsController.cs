using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuickSign.Application.Contracts.DTOs.Documents;
using QuickSign.Application.Contracts.Services;

namespace QuickSign.HttpApi.Controllers;

[Authorize]
[Route("api/v1/stats")]
public sealed class StatisticsController : QuickSignControllerBase
{
    private readonly IStatisticsAppService _statisticsAppService;

    public StatisticsController(IStatisticsAppService statisticsAppService)
    {
        _statisticsAppService = statisticsAppService;
    }

    [HttpGet("overview")]
    public Task<DashboardOverviewStatsDto> GetMyOverviewStatsAsync(CancellationToken cancellationToken)
    {
        return _statisticsAppService.GetMyOverviewStatsAsync(cancellationToken);
    }
}
