namespace QuickSign.Domain.Shared.Certificates;

/// <summary>
/// Constant values and validation boundaries for digital certificates.
/// </summary>
public static class CertificateConsts
{
    /// <summary>
    /// Maximum allowed character length for the certificate subject name.
    /// </summary>
    public const int MaxSubjectNameLength = 255;

    /// <summary>
    /// Maximum allowed character length for the unique X.509 serial number.
    /// </summary>
    public const int MaxSerialNumberLength = 100;

    /// <summary>
    /// Maximum allowed character length for the HashiCorp Vault key storage path.
    /// </summary>
    public const int MaxVaultPathLength = 255;

    /// <summary>
    /// Business rule constraint: A user is limited to a maximum of 5 active certificates concurrently.
    /// </summary>
    public const int MaxActiveCertificatesPerUser = 5;
}
