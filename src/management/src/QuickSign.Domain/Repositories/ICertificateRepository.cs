using QuickSign.Domain.Certificates;

namespace QuickSign.Domain.Repositories;

/// <summary>
/// Repository abstraction for Certificate aggregate roots.
/// </summary>
public interface ICertificateRepository
{
    Task<Certificate?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Certificate?> GetBySerialNumberAsync(string serialNumber, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Certificate>> GetListByUserIdAsync(string userId, CancellationToken cancellationToken = default);
    Task<int> CountActiveByUserIdAsync(string userId, CancellationToken cancellationToken = default);
    Task InsertAsync(Certificate certificate, CancellationToken cancellationToken = default);
    void Update(Certificate certificate);
}
