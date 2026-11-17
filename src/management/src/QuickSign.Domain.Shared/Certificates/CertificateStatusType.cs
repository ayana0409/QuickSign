namespace QuickSign.Domain.Shared.Certificates;

/// <summary>
/// Lifecycle status of a digital certificate.
/// </summary>
public enum CertificateStatusType
{
    /// <summary>
    /// The certificate is currently valid and active for digital signing.
    /// </summary>
    Active = 1,

    /// <summary>
    /// The certificate has passed its expiration timestamp.
    /// </summary>
    Expired = 2,

    /// <summary>
    /// The certificate was explicitly revoked by its owner or an administrator.
    /// </summary>
    Revoked = 3
}
