namespace QuickSign.Application.Contracts.DTOs.ApiKeys;

public sealed class CreateApiKeyRequestDto
{
    public string Name { get; set; } = default!;
    public string Scopes { get; set; } = "sign,verify";
    public DateTimeOffset? ExpiresAt { get; set; }
}
