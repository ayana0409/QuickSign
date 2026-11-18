using System.Text.Json;
using AutoMapper;
using Microsoft.Extensions.Logging;
using QuickSign.Application.Contracts.DTOs.ApiKeys;
using QuickSign.Application.Contracts.Infrastructure;
using QuickSign.Application.Contracts.Services;
using QuickSign.Domain.ApiKeys;
using QuickSign.Domain.Exceptions;
using QuickSign.Domain.Repositories;
using QuickSign.Domain.Shared;

namespace QuickSign.Application.Services;

/// <summary>
/// Application service managing the lifecycle, issuance, and Redis cache synchronization of integration API Keys.
/// </summary>
public sealed class ApiKeyAppService : IApiKeyAppService
{
    private readonly IApiKeyRepository _apiKeyRepository;
    private readonly IApiKeyCryptoService _apiKeyCryptoService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ICacheService _cacheService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<ApiKeyAppService> _logger;

    public ApiKeyAppService(
        IApiKeyRepository apiKeyRepository,
        IApiKeyCryptoService apiKeyCryptoService,
        ICurrentUserService currentUserService,
        ICacheService cacheService,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<ApiKeyAppService> logger)
    {
        _apiKeyRepository = apiKeyRepository;
        _apiKeyCryptoService = apiKeyCryptoService;
        _currentUserService = currentUserService;
        _cacheService = cacheService;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<ApiKeyCreatedResponseDto> CreateApiKeyAsync(
        CreateApiKeyRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var userId = _currentUserService.GetRequiredUserId();
        var (plaintextKey, prefix, hash) = _apiKeyCryptoService.GenerateApiKey();

        var apiKey = new ApiKey(
            Guid.NewGuid(),
            userId,
            request.Name,
            prefix,
            new ApiKeySecretHash(hash),
            request.Scopes,
            request.ExpiresAt);

        await _apiKeyRepository.InsertAsync(apiKey, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Synchronize active API Key metadata to Redis for O(1) verification in Go and .NET
        var cacheKey = $"qs_key:{hash}";
        var cachePayload = JsonSerializer.Serialize(new
        {
            id = apiKey.Id,
            userId = apiKey.UserId,
            scopes = apiKey.Scopes,
            isActive = apiKey.IsActive
        });

        await _cacheService.SetStringAsync(cacheKey, cachePayload, cancellationToken: cancellationToken);

        _logger.LogInformation("API Key {ApiKeyId} created for User {UserId}.", apiKey.Id, userId);

        return new ApiKeyCreatedResponseDto
        {
            Id = apiKey.Id,
            Name = apiKey.Name,
            KeyPrefix = apiKey.KeyPrefix,
            PlaintextApiKey = plaintextKey,
            Scopes = apiKey.Scopes,
            ExpiresAt = apiKey.ExpiresAt,
            CreatedAt = apiKey.CreatedAt
        };
    }

    public async Task RevokeApiKeyAsync(Guid apiKeyId, CancellationToken cancellationToken = default)
    {
        var userId = _currentUserService.GetRequiredUserId();
        var apiKey = await _apiKeyRepository.GetByIdAsync(apiKeyId, cancellationToken)
            ?? throw new BusinessException(
                QuickSignDomainErrorCodes.ApiKeyNotFound,
                "API Key not found.",
                404);

        if (apiKey.UserId != userId && !_currentUserService.IsInRole("Admin"))
        {
            throw new BusinessException(
                QuickSignDomainErrorCodes.ForbiddenAccess,
                "You are not authorized to revoke this API Key.",
                403);
        }

        apiKey.Revoke();
        _apiKeyRepository.Update(apiKey);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Remove from Redis cache
        await _cacheService.RemoveAsync($"qs_key:{apiKey.KeyHash.Value}", cancellationToken);

        _logger.LogInformation("API Key {ApiKeyId} revoked for User {UserId}.", apiKeyId, userId);
    }

    public async Task<IReadOnlyList<ApiKeyItemDto>> GetMyApiKeysAsync(CancellationToken cancellationToken = default)
    {
        var userId = _currentUserService.GetRequiredUserId();
        var keys = await _apiKeyRepository.GetListByUserIdAsync(userId, cancellationToken);
        return _mapper.Map<IReadOnlyList<ApiKeyItemDto>>(keys);
    }
}
