using QuickSign.Application.Contracts.DTOs.ApiKeys;

namespace QuickSign.Application.Contracts.Services;

public interface IApiKeyAppService
{
    Task<ApiKeyCreatedResponseDto> CreateApiKeyAsync(CreateApiKeyRequestDto request, CancellationToken cancellationToken = default);
    Task RevokeApiKeyAsync(Guid apiKeyId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ApiKeyItemDto>> GetMyApiKeysAsync(CancellationToken cancellationToken = default);
}
