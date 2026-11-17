using QuickSign.Domain.Exceptions;
using QuickSign.Domain.Repositories;
using QuickSign.Domain.Shared;
using QuickSign.Domain.Shared.Certificates;

namespace QuickSign.Domain.Certificates;

/// <summary>
/// Domain service implementation ensuring digital certificate business invariants before creation.
/// </summary>
public sealed class CertificateManager : ICertificateManager
{
    private readonly ICertificateRepository _certificateRepository;

    public CertificateManager(ICertificateRepository certificateRepository)
    {
        _certificateRepository = certificateRepository;
    }

    public async Task<Certificate> CreateAsync(
        Guid id,
        string userId,
        string subjectName,
        string serialNumber,
        KeyAlgorithmType algorithm,
        string publicKeyPem,
        string vaultKeyPath,
        DateTimeOffset expiresAt,
        CancellationToken cancellationToken = default)
    {
        // Enforce maximum active certificates policy (limit = 5)
        var activeCount = await _certificateRepository.CountActiveByUserIdAsync(userId, cancellationToken);
        if (activeCount >= CertificateConsts.MaxActiveCertificatesPerUser)
        {
            throw new BusinessException(
                QuickSignDomainErrorCodes.MaxActiveCertsReached,
                $"Maximum active certificates limit ({CertificateConsts.MaxActiveCertificatesPerUser}) reached for user.",
                400,
                new Dictionary<string, object>
                {
                    ["limit"] = CertificateConsts.MaxActiveCertificatesPerUser,
                    ["activeCount"] = activeCount
                });
        }

        return new Certificate(
            id,
            userId,
            subjectName,
            serialNumber,
            algorithm,
            publicKeyPem,
            vaultKeyPath,
            expiresAt);
    }
}
