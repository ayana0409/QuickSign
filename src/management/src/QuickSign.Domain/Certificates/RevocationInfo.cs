using QuickSign.Domain.SeedWork;

namespace QuickSign.Domain.Certificates;

/// <summary>
/// Value Object encapsulating cryptographic revocation metadata for a certificate.
/// </summary>
public sealed class RevocationInfo : ValueObject
{
    public DateTimeOffset RevokedAt { get; }
    public DateTimeOffset InvalidatedFrom { get; }
    public string? Reason { get; }

    public RevocationInfo(DateTimeOffset revokedAt, DateTimeOffset invalidatedFrom, string? reason)
    {
        RevokedAt = revokedAt;
        InvalidatedFrom = invalidatedFrom;
        Reason = reason?.Trim();
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return RevokedAt;
        yield return InvalidatedFrom;
        yield return Reason;
    }
}
