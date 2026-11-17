namespace QuickSign.Domain.Data;

/// <summary>
/// Contributor contract for initial data seeding routines.
/// </summary>
public interface IDataSeedContributor
{
    Task SeedAsync(CancellationToken cancellationToken = default);
}
