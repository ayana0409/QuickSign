using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuickSign.Application.Contracts.DTOs.Documents;
using QuickSign.Application.Contracts.Services;

namespace QuickSign.HttpApi.Controllers;

[Authorize]
[Route("api/v1/documents")]
public sealed class DocumentsController : QuickSignControllerBase
{
    private readonly IDocumentAppService _documentAppService;

    public DocumentsController(IDocumentAppService documentAppService)
    {
        _documentAppService = documentAppService;
    }

    [HttpGet]
    public Task<IReadOnlyList<SignedDocumentDto>> GetMyDocumentsAsync(
        [FromQuery] int skipCount = 0,
        [FromQuery] int maxResultCount = 20,
        CancellationToken cancellationToken = default)
    {
        return _documentAppService.GetMyDocumentsAsync(skipCount, maxResultCount, cancellationToken);
    }

    [HttpGet("{id:guid}")]
    public Task<SignedDocumentDto> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return _documentAppService.GetByIdAsync(id, cancellationToken);
    }

    [HttpGet("code/{documentCode}")]
    [AllowAnonymous]
    public Task<SignedDocumentDto> GetByCodeAsync(string documentCode, CancellationToken cancellationToken)
    {
        return _documentAppService.GetByCodeAsync(documentCode, cancellationToken);
    }

    [HttpGet("{id:guid}/verifications")]
    public Task<IReadOnlyList<VerificationLogDto>> GetVerificationHistoryAsync(
        Guid id,
        [FromQuery] int skipCount = 0,
        [FromQuery] int maxResultCount = 50,
        CancellationToken cancellationToken = default)
    {
        return _documentAppService.GetVerificationHistoryAsync(id, skipCount, maxResultCount, cancellationToken);
    }
}
