namespace QuickSign.Application.Contracts.DTOs.ApiKeys;

public sealed class ApiKeyCreatedResponseDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = default!;
    public string KeyPrefix { get; set; } = default!;
    public string PlaintextApiKey { get; set; } = default!;
    public string Scopes { get; set; } = default!;
    public DateTimeOffset? ExpiresAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
