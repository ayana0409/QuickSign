namespace QuickSign.Infrastructure.Identity;

/// <summary>
/// Configuration options for connecting to the external SSO Identity Server.
/// All URLs and credentials must be explicitly provided in configuration without silent fallbacks.
/// </summary>
public sealed class SsoIdentityOptions
{
    public const string SectionName = "SsoIdentity";

    public string Authority { get; set; } = default!;
    public string TokenEndpoint { get; set; } = default!;
    public string RegisterEndpoint { get; set; } = default!;
    public string ClientId { get; set; } = default!;
    public string? ClientSecret { get; set; }
    public string Scope { get; set; } = "openid profile email roles offline_access";
}
