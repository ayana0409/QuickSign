namespace QuickSign.Domain.Data;

/// <summary>
/// Domain service coordinating database schema migration and foundational data seeding.
/// </summary>
public sealed class QuickSignDbMigrationService
{
    private readonly IQuickSignDbSchemaMigrator _schemaMigrator;
    private readonly IEnumerable<IDataSeedContributor> _seedContributors;

    public QuickSignDbMigrationService(
        IQuickSignDbSchemaMigrator schemaMigrator,
        IEnumerable<IDataSeedContributor> seedContributors)
    {
        _schemaMigrator = schemaMigrator;
        _seedContributors = seedContributors;
    }

    public async Task MigrateAsync(CancellationToken cancellationToken = default)
    {
        // 1. Apply database schema migrations
        await _schemaMigrator.MigrateAsync(cancellationToken);

        // 2. Execute any registered seed contributors
        foreach (var contributor in _seedContributors)
        {
            await contributor.SeedAsync(cancellationToken);
        }
    }
}
