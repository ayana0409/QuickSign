using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuickSign.Application.Contracts.DTOs.Certificates;
using QuickSign.Application.Contracts.Services;

namespace QuickSign.HttpApi.Controllers;

[Authorize]
[Route("api/v1/certificates")]
public sealed class CertificatesController : QuickSignControllerBase
{
    private readonly ICertificateAppService _certificateAppService;

    public CertificatesController(ICertificateAppService certificateAppService)
    {
        _certificateAppService = certificateAppService;
    }

    [HttpGet]
    public Task<IReadOnlyList<CertificateResponseDto>> GetMyCertificatesAsync(CancellationToken cancellationToken)
    {
        return _certificateAppService.GetMyCertificatesAsync(cancellationToken);
    }

    [HttpGet("{id:guid}")]
    public Task<CertificateResponseDto> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return _certificateAppService.GetByIdAsync(id, cancellationToken);
    }

    [HttpPost]
    public Task<CertificateResponseDto> IssueCertificateAsync([FromBody] IssueCertificateRequestDto input, CancellationToken cancellationToken)
    {
        return _certificateAppService.IssueCertificateAsync(input, cancellationToken);
    }

    [HttpPut("{id:guid}/revoke")]
    public async Task<IActionResult> RevokeCertificateAsync(Guid id, [FromBody] RevokeCertificateRequestDto input, CancellationToken cancellationToken)
    {
        await _certificateAppService.RevokeCertificateAsync(id, input, cancellationToken);
        return NoContent();
    }
}
