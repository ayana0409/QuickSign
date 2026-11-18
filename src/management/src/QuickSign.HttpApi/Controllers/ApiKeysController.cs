using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuickSign.Application.Contracts.DTOs.ApiKeys;
using QuickSign.Application.Contracts.Services;

namespace QuickSign.HttpApi.Controllers;

[Authorize]
[Route("api/v1/api-keys")]
public sealed class ApiKeysController : QuickSignControllerBase
{
    private readonly IApiKeyAppService _apiKeyAppService;

    public ApiKeysController(IApiKeyAppService apiKeyAppService)
    {
        _apiKeyAppService = apiKeyAppService;
    }

    [HttpGet]
    public Task<IReadOnlyList<ApiKeyItemDto>> GetMyApiKeysAsync(CancellationToken cancellationToken)
    {
        return _apiKeyAppService.GetMyApiKeysAsync(cancellationToken);
    }

    [HttpPost]
    public Task<ApiKeyCreatedResponseDto> CreateApiKeyAsync([FromBody] CreateApiKeyRequestDto input, CancellationToken cancellationToken)
    {
        return _apiKeyAppService.CreateApiKeyAsync(input, cancellationToken);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> RevokeApiKeyAsync(Guid id, CancellationToken cancellationToken)
    {
        await _apiKeyAppService.RevokeApiKeyAsync(id, cancellationToken);
        return NoContent();
    }
}
