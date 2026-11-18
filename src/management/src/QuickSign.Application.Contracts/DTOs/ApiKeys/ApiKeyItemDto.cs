namespace QuickSign.Application.Contracts.DTOs.ApiKeys;

public sealed class ApiKeyItemDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = default!;
    public string KeyPrefix { get; set; } = default!;
    public string Scopes { get; set; } = default!;
    public bool IsActive { get; set; }
    public DateTimeOffset? LastUsedAt { get; set; }
    public DateTimeOffset? ExpiresAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
