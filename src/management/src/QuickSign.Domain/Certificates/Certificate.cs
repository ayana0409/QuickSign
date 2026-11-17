using QuickSign.Domain.Exceptions;
using QuickSign.Domain.SeedWork;
using QuickSign.Domain.Shared;
using QuickSign.Domain.Shared.Certificates;

namespace QuickSign.Domain.Certificates;

/// <summary>
/// Digital Certificate Aggregate Root managing identity, cryptographic keys, and lifecycle status.
/// </summary>
public sealed class Certificate : AggregateRoot<Guid>
{
    public string UserId { get; private set; } = default!;
    public string SubjectName { get; private set; } = default!;
    public string SerialNumber { get; private set; } = default!;
    public KeyAlgorithmType Algorithm { get; private set; }
    public string PublicKeyPem { get; private set; } = default!;
    public string VaultKeyPath { get; private set; } = default!;
    public CertificateStatusType Status { get; private set; }
    public RevocationInfo? Revocation { get; private set; }
    public DateTimeOffset ExpiresAt { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>
    /// Parameterless constructor required by Entity Framework Core.
    /// </summary>
    private Certificate() { }

    /// <summary>
    /// Internal constructor invoked exclusively via ICertificateManager domain service.
    /// </summary>
    internal Certificate(
        Guid id,
        string userId,
        string subjectName,
        string serialNumber,
        KeyAlgorithmType algorithm,
        string publicKeyPem,
        string vaultKeyPath,
        DateTimeOffset expiresAt) : base(id)
    {
        if (string.IsNullOrWhiteSpace(userId))
            throw new ArgumentException("UserId cannot be empty.", nameof(userId));

        if (string.IsNullOrWhiteSpace(subjectName))
            throw new ArgumentException("SubjectName cannot be empty.", nameof(subjectName));

        if (string.IsNullOrWhiteSpace(serialNumber))
            throw new ArgumentException("SerialNumber cannot be empty.", nameof(serialNumber));

        if (string.IsNullOrWhiteSpace(publicKeyPem))
            throw new ArgumentException("PublicKeyPem cannot be empty.", nameof(publicKeyPem));

        if (string.IsNullOrWhiteSpace(vaultKeyPath))
            throw new ArgumentException("VaultKeyPath cannot be empty.", nameof(vaultKeyPath));

        UserId = userId.Trim();
        SubjectName = subjectName.Trim();
        SerialNumber = serialNumber.Trim();
        Algorithm = algorithm;
        PublicKeyPem = publicKeyPem.Trim();
        VaultKeyPath = vaultKeyPath.Trim();
        Status = CertificateStatusType.Active;
        CreatedAt = DateTimeOffset.UtcNow;
        ExpiresAt = expiresAt;
    }

    /// <summary>
    /// Revokes the certificate with an explicit invalidation timestamp and reason.
    /// </summary>
    /// <param name="invalidatedFrom">The point in time from which signatures are deemed invalid.</param>
    /// <param name="reason">Human-readable reason for certificate revocation.</param>
    public void Revoke(DateTimeOffset invalidatedFrom, string? reason)
    {
        if (Status == CertificateStatusType.Revoked)
        {
            throw new BusinessException(
                QuickSignDomainErrorCodes.CertAlreadyRevoked,
                "Certificate has already been revoked.",
                400);
        }

        if (invalidatedFrom < CreatedAt)
        {
            throw new BusinessException(
                QuickSignDomainErrorCodes.InvalidRevocationTimestamp,
                "Invalidation timestamp cannot be earlier than certificate creation time.",
                400);
        }

        Status = CertificateStatusType.Revoked;
        Revocation = new RevocationInfo(DateTimeOffset.UtcNow, invalidatedFrom, reason);
    }

    /// <summary>
    /// Marks the certificate as expired if current time exceeds expiration boundary.
    /// </summary>
    public void CheckAndMarkExpired()
    {
        if (Status == CertificateStatusType.Active && DateTimeOffset.UtcNow >= ExpiresAt)
        {
            Status = CertificateStatusType.Expired;
        }
    }
}
