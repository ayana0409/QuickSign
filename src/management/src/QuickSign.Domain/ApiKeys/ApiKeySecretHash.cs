using QuickSign.Domain.SeedWork;
using QuickSign.Domain.Shared.ApiKeys;

namespace QuickSign.Domain.ApiKeys;

/// <summary>
/// Value Object encapsulating a SHA-256 cryptographic hash of an API key secret.
/// </summary>
public sealed class ApiKeySecretHash : ValueObject
{
    public string Value { get; }

    public ApiKeySecretHash(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Trim().Length != ApiKeyConsts.Sha256HexLength)
        {
            throw new ArgumentException(
                $"API key secret hash must be exactly {ApiKeyConsts.Sha256HexLength} hexadecimal characters.",
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
