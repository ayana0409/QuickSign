using Microsoft.EntityFrameworkCore;
using QuickSign.Domain.Data;

namespace QuickSign.EntityFrameworkCore;

/// <summary>
/// Entity Framework Core implementation of the database schema migrator.
/// Applies pending migrations to the PostgreSQL database.
/// </summary>
public sealed class EfCoreQuickSignDbSchemaMigrator : IQuickSignDbSchemaMigrator
{
    private readonly QuickSignDbContext _dbContext;

    public EfCoreQuickSignDbSchemaMigrator(QuickSignDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task MigrateAsync(CancellationToken cancellationToken = default)
    {
        await _dbContext.Database.MigrateAsync(cancellationToken);
    }
}
