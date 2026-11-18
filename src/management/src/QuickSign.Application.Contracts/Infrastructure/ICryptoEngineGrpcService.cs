using QuickSign.Application.Contracts.DTOs.Certificates;

namespace QuickSign.Application.Contracts.Infrastructure;

/// <summary>
/// Infrastructure gateway contract communicating with the Golang Crypto Engine via gRPC.
/// Supports cryptographic key generation and compensating rollback deletion.
/// </summary>
public interface ICryptoEngineGrpcService
{
    Task<GeneratedKeyMaterialDto> GenerateCertificateKeyPairAsync(
        Guid certificateId,
        string userId,
        string subjectName,
        string algorithm,
        DateTimeOffset validFrom,
        DateTimeOffset expiresAt,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Compensating transaction to delete an orphaned private key from HashiCorp Vault if DB persistence fails.
    /// </summary>
    Task<bool> DeleteCertificateKeyPairAsync(
        string vaultKeyPath,
        CancellationToken cancellationToken = default);
}
