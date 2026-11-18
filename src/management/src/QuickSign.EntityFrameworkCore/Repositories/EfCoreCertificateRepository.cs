using Microsoft.EntityFrameworkCore;
using QuickSign.Domain.Certificates;
using QuickSign.Domain.Repositories;
using QuickSign.Domain.Shared.Certificates;

namespace QuickSign.EntityFrameworkCore.Repositories;

/// <summary>
/// Entity Framework Core repository implementation for Certificate aggregate root.
/// Uses .AsNoTracking() on read operations for optimal memory and query throughput.
/// </summary>
public sealed class EfCoreCertificateRepository : ICertificateRepository
{
    private readonly QuickSignDbContext _dbContext;

    public EfCoreCertificateRepository(QuickSignDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<Certificate?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _dbContext.Certificates
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public Task<Certificate?> GetBySerialNumberAsync(string serialNumber, CancellationToken cancellationToken = default)
    {
        return _dbContext.Certificates
            .FirstOrDefaultAsync(x => x.SerialNumber == serialNumber, cancellationToken);
    }

    public async Task<IReadOnlyList<Certificate>> GetListByUserIdAsync(string userId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Certificates
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public Task<int> CountActiveByUserIdAsync(string userId, CancellationToken cancellationToken = default)
    {
        return _dbContext.Certificates
            .CountAsync(x => x.UserId == userId && x.Status == CertificateStatusType.Active, cancellationToken);
    }

    public async Task InsertAsync(Certificate certificate, CancellationToken cancellationToken = default)
    {
        await _dbContext.Certificates.AddAsync(certificate, cancellationToken);
    }

    public void Update(Certificate certificate)
    {
        _dbContext.Certificates.Update(certificate);
    }
}
