using Microsoft.EntityFrameworkCore;
using QuickSign.Domain.ApiKeys;
using QuickSign.Domain.Repositories;

namespace QuickSign.EntityFrameworkCore.Repositories;

/// <summary>
/// Entity Framework Core repository implementation for ApiKey aggregate root.
/// </summary>
public sealed class EfCoreApiKeyRepository : IApiKeyRepository
{
    private readonly QuickSignDbContext _dbContext;

    public EfCoreApiKeyRepository(QuickSignDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<ApiKey?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _dbContext.ApiKeys
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public Task<ApiKey?> GetByHashAsync(string keyHash, CancellationToken cancellationToken = default)
    {
        var targetHash = new ApiKeySecretHash(keyHash);
        return _dbContext.ApiKeys
            .FirstOrDefaultAsync(x => x.KeyHash == targetHash, cancellationToken);
    }

    public async Task<IReadOnlyList<ApiKey>> GetListByUserIdAsync(string userId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.ApiKeys
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task InsertAsync(ApiKey apiKey, CancellationToken cancellationToken = default)
    {
        await _dbContext.ApiKeys.AddAsync(apiKey, cancellationToken);
    }

    public void Update(ApiKey apiKey)
    {
        _dbContext.ApiKeys.Update(apiKey);
    }

    public void Delete(ApiKey apiKey)
    {
        _dbContext.ApiKeys.Remove(apiKey);
    }
}
