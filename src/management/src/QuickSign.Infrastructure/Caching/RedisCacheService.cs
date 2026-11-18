using Microsoft.Extensions.Logging;
using QuickSign.Application.Contracts.Infrastructure;
using StackExchange.Redis;

namespace QuickSign.Infrastructure.Caching;

/// <summary>
/// Distributed caching service implementation backed by Redis.
/// </summary>
public sealed class RedisCacheService : ICacheService
{
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<RedisCacheService> _logger;

    public RedisCacheService(
        IConnectionMultiplexer redis,
        ILogger<RedisCacheService> logger)
    {
        _redis = redis;
        _logger = logger;
    }

    private IDatabase Database => _redis.GetDatabase();

    public async Task<string?> GetStringAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            var value = await Database.StringGetAsync(key);
            return value.HasValue ? value.ToString() : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve cache key {Key} from Redis.", key);
            return null; // Graceful degradation on cache failure
        }
    }

    public async Task SetStringAsync(string key, string value, TimeSpan? expiry = null, CancellationToken cancellationToken = default)
    {
        try
        {
            if (expiry.HasValue)
            {
                await Database.StringSetAsync(key, value, expiry.Value);
            }
            else
            {
                await Database.StringSetAsync(key, value);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set cache key {Key} in Redis.", key);
        }
    }

    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            await Database.KeyDeleteAsync(key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete cache key {Key} from Redis.", key);
        }
    }
}
