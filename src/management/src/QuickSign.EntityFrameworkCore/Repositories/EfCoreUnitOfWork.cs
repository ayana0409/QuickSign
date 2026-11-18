using QuickSign.Domain.Repositories;

namespace QuickSign.EntityFrameworkCore.Repositories;

/// <summary>
/// Entity Framework Core implementation of the Unit of Work pattern.
/// </summary>
public sealed class EfCoreUnitOfWork : IUnitOfWork
{
    private readonly QuickSignDbContext _dbContext;

    public EfCoreUnitOfWork(QuickSignDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }
}
