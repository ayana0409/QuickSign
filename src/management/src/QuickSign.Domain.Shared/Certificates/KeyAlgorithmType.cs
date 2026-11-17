namespace QuickSign.Domain.Shared.Certificates;

/// <summary>
/// Supported asymmetric cryptographic algorithms for key pairs.
/// </summary>
public enum KeyAlgorithmType
{
    /// <summary>
    /// RSA 2048-bit asymmetric key pair with SHA-256 digest.
    /// </summary>
    Rsa2048 = 1,

    /// <summary>
    /// ECDSA NIST P-256 elliptic curve asymmetric key pair.
    /// </summary>
    EcdsaP256 = 2
}
