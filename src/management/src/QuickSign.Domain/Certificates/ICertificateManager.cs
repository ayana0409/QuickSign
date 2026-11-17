using QuickSign.Domain.Shared.Certificates;

namespace QuickSign.Domain.Certificates;

/// <summary>
/// Domain service interface governing Certificate aggregate creation and domain invariants.
/// </summary>
public interface ICertificateManager
{
    Task<Certificate> CreateAsync(
        Guid id,
        string userId,
        string subjectName,
        string serialNumber,
        KeyAlgorithmType algorithm,
        string publicKeyPem,
        string vaultKeyPath,
        DateTimeOffset expiresAt,
        CancellationToken cancellationToken = default);
}
