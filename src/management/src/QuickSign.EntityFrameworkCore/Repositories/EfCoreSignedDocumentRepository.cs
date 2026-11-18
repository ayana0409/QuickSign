using Microsoft.EntityFrameworkCore;
using QuickSign.Domain.Documents;
using QuickSign.Domain.Repositories;

namespace QuickSign.EntityFrameworkCore.Repositories;

/// <summary>
/// Entity Framework Core repository implementation for SignedDocument aggregate root and verification audit logs.
/// </summary>
public sealed class EfCoreSignedDocumentRepository : ISignedDocumentRepository
{
    private readonly QuickSignDbContext _dbContext;

    public EfCoreSignedDocumentRepository(QuickSignDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<SignedDocument?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _dbContext.SignedDocuments
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public Task<SignedDocument?> GetByCodeAsync(string documentCode, CancellationToken cancellationToken = default)
    {
        var targetCode = new DocumentCode(documentCode);
        return _dbContext.SignedDocuments
            .FirstOrDefaultAsync(x => x.DocumentCode == targetCode, cancellationToken);
    }

    public async Task<IReadOnlyList<SignedDocument>> GetListByUserIdAsync(
        string userId,
        int skipCount = 0,
        int maxResultCount = 20,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.SignedDocuments
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.CreatedAt)
            .Skip(skipCount)
            .Take(maxResultCount)
            .ToListAsync(cancellationToken);
    }

    public Task<int> GetCountByUserIdAsync(string userId, CancellationToken cancellationToken = default)
    {
        return _dbContext.SignedDocuments
            .CountAsync(x => x.UserId == userId, cancellationToken);
    }

    public async Task<IReadOnlyList<VerificationLog>> GetVerificationLogsAsync(
        Guid signedDocumentId,
        int skipCount = 0,
        int maxResultCount = 50,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.VerificationLogs
            .AsNoTracking()
            .Where(x => x.SignedDocumentId == signedDocumentId)
            .OrderByDescending(x => x.VerifiedAt)
            .Skip(skipCount)
            .Take(maxResultCount)
            .ToListAsync(cancellationToken);
    }

    public async Task InsertAsync(SignedDocument document, CancellationToken cancellationToken = default)
    {
        await _dbContext.SignedDocuments.AddAsync(document, cancellationToken);
    }

    public async Task InsertVerificationLogAsync(VerificationLog log, CancellationToken cancellationToken = default)
    {
        await _dbContext.VerificationLogs.AddAsync(log, cancellationToken);
    }

    public void Update(SignedDocument document)
    {
        _dbContext.SignedDocuments.Update(document);
    }
}
