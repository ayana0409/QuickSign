namespace QuickSign.Domain.SeedWork;

/// <summary>
/// Base class for Aggregate Roots in Tactical DDD pattern.
/// Aggregate roots serve as consistency and transaction boundaries.
/// </summary>
/// <typeparam name="TKey">Primary key type.</typeparam>
public abstract class AggregateRoot<TKey> : Entity<TKey>
    where TKey : notnull
{
    protected AggregateRoot() { }

    protected AggregateRoot(TKey id) : base(id) { }
}
