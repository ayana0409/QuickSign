using QuickSign.Domain.SeedWork;

namespace QuickSign.Domain.ApiKeys;

/// <summary>
/// ApiKey Aggregate Root managing external application integration credentials and scopes.
/// </summary>
public sealed class ApiKey : AggregateRoot<Guid>
{
    public string UserId { get; private set; } = default!;
    public string Name { get; private set; } = default!;
    public string KeyPrefix { get; private set; } = default!;
    public ApiKeySecretHash KeyHash { get; private set; } = default!;
    public string Scopes { get; private set; } = default!;
    public bool IsActive { get; private set; }
    public DateTimeOffset? LastUsedAt { get; private set; }
    public DateTimeOffset? ExpiresAt { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    private ApiKey() { }

    public ApiKey(
        Guid id,
        string userId,
        string name,
        string keyPrefix,
        ApiKeySecretHash keyHash,
        string scopes,
        DateTimeOffset? expiresAt = null) : base(id)
    {
        if (string.IsNullOrWhiteSpace(userId))
            throw new ArgumentException("UserId cannot be empty.", nameof(userId));

        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name cannot be empty.", nameof(name));

        if (string.IsNullOrWhiteSpace(keyPrefix))
            throw new ArgumentException("KeyPrefix cannot be empty.", nameof(keyPrefix));

        if (string.IsNullOrWhiteSpace(scopes))
            throw new ArgumentException("Scopes cannot be empty.", nameof(scopes));

        UserId = userId.Trim();
        Name = name.Trim();
        KeyPrefix = keyPrefix.Trim();
        KeyHash = keyHash ?? throw new ArgumentNullException(nameof(keyHash));
        Scopes = scopes.Trim().ToLowerInvariant();
        IsActive = true;
        CreatedAt = DateTimeOffset.UtcNow;
        ExpiresAt = expiresAt;
    }

    /// <summary>
    /// Revokes and permanently deactivates the API key.
    /// </summary>
    public void Revoke()
    {
        IsActive = false;
    }

    /// <summary>
    /// Updates the last used timestamp when an authenticated API call occurs.
    /// </summary>
    public void RecordUsage(DateTimeOffset timestamp)
    {
        LastUsedAt = timestamp;
    }

    /// <summary>
    /// Checks if the API key contains a specific scope (e.g., 'sign', 'verify').
    /// </summary>
    public bool HasScope(string scope)
    {
        if (string.IsNullOrWhiteSpace(scope))
            return false;

        var scopeList = Scopes.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return scopeList.Contains(scope.Trim(), StringComparer.OrdinalIgnoreCase);
    }
}
