namespace QuickSign.Domain.SeedWork;

/// <summary>
/// Base class for all domain entities encapsulating identity.
/// </summary>
/// <typeparam name="TKey">Primary key type (e.g., Guid, long).</typeparam>
public abstract class Entity<TKey>
    where TKey : notnull
{
    public virtual TKey Id { get; protected set; } = default!;

    protected Entity() { }

    protected Entity(TKey id)
    {
        Id = id;
    }

    public override bool Equals(object? obj)
    {
        if (obj is not Entity<TKey> other)
            return false;

        if (ReferenceEquals(this, other))
            return true;

        if (GetType() != other.GetType())
            return false;

        if (EqualityComparer<TKey>.Default.Equals(Id, default!) || EqualityComparer<TKey>.Default.Equals(other.Id, default!))
            return false;

        return EqualityComparer<TKey>.Default.Equals(Id, other.Id);
    }

    public override int GetHashCode()
    {
        return (GetType().ToString() + Id).GetHashCode();
    }

    public static bool operator ==(Entity<TKey>? left, Entity<TKey>? right)
    {
        if (left is null)
            return right is null;

        return left.Equals(right);
    }

    public static bool operator !=(Entity<TKey>? left, Entity<TKey>? right)
    {
        return !(left == right);
    }
}
