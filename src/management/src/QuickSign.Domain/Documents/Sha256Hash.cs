using QuickSign.Domain.SeedWork;

namespace QuickSign.Domain.Documents;

/// <summary>
/// Value Object encapsulating a 64-character hexadecimal representation of a SHA-256 digest.
/// </summary>
public sealed class Sha256Hash : ValueObject
{
    public const int Length = 64;

    public string Value { get; }

    public Sha256Hash(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Trim().Length != Length)
        {
            throw new ArgumentException(
                $"SHA-256 hash must be exactly {Length} hexadecimal characters.",
                nameof(value));
        }

        Value = value.Trim().ToLowerInvariant();
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;
}
