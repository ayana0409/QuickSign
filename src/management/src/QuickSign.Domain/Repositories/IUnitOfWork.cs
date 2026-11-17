namespace QuickSign.Domain.Repositories;

/// <summary>
/// Unit of Work contract maintaining transaction boundaries across aggregate persistence.
/// </summary>
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
