using QuickSign.Domain.SeedWork;

namespace QuickSign.Domain.Documents;

/// <summary>
/// Value Object representing a unique QuickSign document identifier (e.g. QS-2026-8F9A2B).
/// Embedded in PDF PAdES authenticated CMS attributes and JSON JWS header.
/// </summary>
public sealed class DocumentCode : ValueObject
{
    public string Value { get; }

    public DocumentCode(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Document code cannot be empty.", nameof(value));

        Value = value.Trim().ToUpperInvariant();
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;
}
