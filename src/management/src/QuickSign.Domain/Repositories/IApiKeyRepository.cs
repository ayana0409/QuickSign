using QuickSign.Domain.ApiKeys;

namespace QuickSign.Domain.Repositories;

/// <summary>
/// Repository abstraction for ApiKey aggregate roots.
/// </summary>
public interface IApiKeyRepository
{
    Task<ApiKey?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ApiKey?> GetByHashAsync(string keyHash, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ApiKey>> GetListByUserIdAsync(string userId, CancellationToken cancellationToken = default);
    Task InsertAsync(ApiKey apiKey, CancellationToken cancellationToken = default);
    void Update(ApiKey apiKey);
    void Delete(ApiKey apiKey);
}
