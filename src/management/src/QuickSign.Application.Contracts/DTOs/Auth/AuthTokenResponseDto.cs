using System.Text.Json.Serialization;

namespace QuickSign.Application.Contracts.DTOs.Auth;

/// <summary>
/// OAuth 2.0 Token Response DTO for SSO Identity Provider payload.
/// </summary>
public sealed class AuthTokenResponseDto
{
    [JsonPropertyName("access_token")]
    public string AccessToken { get; set; } = default!;

    [JsonPropertyName("token_type")]
    public string TokenType { get; set; } = "Bearer";

    [JsonPropertyName("expires_in")]
    public int ExpiresIn { get; set; }

    [JsonPropertyName("refresh_token")]
    public string? RefreshToken { get; set; }

    [JsonPropertyName("id_token")]
    public string? IdToken { get; set; }

    [JsonPropertyName("scope")]
    public string? Scope { get; set; }
}
