namespace QuickSign.Domain.Shared.ApiKeys;

/// <summary>
/// Constants and constraints governing API keys for automated integrations.
/// </summary>
public static class ApiKeyConsts
{
    /// <summary>
    /// Standard live prefix for generated API keys.
    /// </summary>
    public const string LiveKeyPrefix = "qs_live_";

    /// <summary>
    /// Number of visible prefix characters displayed in user interfaces.
    /// </summary>
    public const int DisplayPrefixLength = 8;

    /// <summary>
    /// Cryptographic random entropy byte count (yielding 64 hex characters or 43 base64 characters).
    /// </summary>
    public const int SecretByteLength = 32;

    /// <summary>
    /// Maximum character length for API key friendly names.
    /// </summary>
    public const int MaxNameLength = 150;

    /// <summary>
    /// Maximum character length for API key prefix column.
    /// </summary>
    public const int MaxPrefixColumnLength = 20;

    /// <summary>
    /// Standard length for SHA-256 hexadecimal hash representation.
    /// </summary>
    public const int Sha256HexLength = 64;

    /// <summary>
    /// Maximum character length for scope definitions.
    /// </summary>
    public const int MaxScopesLength = 100;
}
