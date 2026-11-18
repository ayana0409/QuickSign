using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuickSign.Application.Contracts.DTOs.Certificates;
using QuickSign.Application.Contracts.DTOs.Documents;
using QuickSign.Application.Contracts.Services;

namespace QuickSign.HttpApi.Controllers;

[Authorize(Roles = "Admin,admin,sso-admin")]
[Route("api/v1/admin")]
public sealed class AdminController : QuickSignControllerBase
{
    private readonly IAdminAppService _adminAppService;

    public AdminController(IAdminAppService adminAppService)
    {
        _adminAppService = adminAppService;
    }

    [HttpGet("stats/overview")]
    public Task<DashboardOverviewStatsDto> GetSystemOverviewStatsAsync(CancellationToken cancellationToken)
    {
        return _adminAppService.GetSystemOverviewStatsAsync(cancellationToken);
    }

    [HttpPut("certificates/{id:guid}/revoke")]
    public async Task<IActionResult> RevokeAnyCertificateAsync(
        Guid id,
        [FromBody] RevokeCertificateRequestDto input,
        CancellationToken cancellationToken)
    {
        await _adminAppService.RevokeAnyCertificateAsync(id, input, cancellationToken);
        return NoContent();
    }
}
