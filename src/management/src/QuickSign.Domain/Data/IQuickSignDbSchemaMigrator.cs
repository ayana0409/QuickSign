namespace QuickSign.Domain.Data;

/// <summary>
/// Abstraction for applying relational database schema migrations.
/// Implemented by EfCoreQuickSignDbSchemaMigrator in EntityFrameworkCore layer.
/// </summary>
public interface IQuickSignDbSchemaMigrator
{
    Task MigrateAsync(CancellationToken cancellationToken = default);
}
