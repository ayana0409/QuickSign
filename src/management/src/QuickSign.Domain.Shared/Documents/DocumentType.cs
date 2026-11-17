namespace QuickSign.Domain.Shared.Documents;

/// <summary>
/// Supported digital document formats for cryptographic signing.
/// </summary>
public enum DocumentType
{
    /// <summary>
    /// Portable Document Format (signed via PAdES ETSI EN 319 142).
    /// </summary>
    Pdf = 1,

    /// <summary>
    /// JavaScript Object Notation (signed via JWS RFC 7515).
    /// </summary>
    Json = 2
}
