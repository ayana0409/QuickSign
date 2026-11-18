using System.Security.Cryptography;
using System.Text;
using QuickSign.Application.Contracts.Infrastructure;
using QuickSign.Domain.Shared.ApiKeys;

namespace QuickSign.Infrastructure.Security;

/// <summary>
/// Cryptographic security utility for generating high-entropy API keys and computing SHA-256 digests.
/// </summary>
public sealed class ApiKeyCryptoService : IApiKeyCryptoService
{
    public (string PlaintextKey, string Prefix, string Hash) GenerateApiKey()
    {
        // 1. Generate 32 bytes of cryptographically secure random entropy
        var randomBytes = new byte[ApiKeyConsts.SecretByteLength];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(randomBytes);
        }

        var secretHex = Convert.ToHexString(randomBytes).ToLowerInvariant();

        // 2. Format complete plaintext API key: qs_live_<hex>
        var plaintextKey = $"{ApiKeyConsts.LiveKeyPrefix}{secretHex}";

        // 3. Extract display prefix: first 8 characters (e.g. qs_live_) or prefix + first 4 hex
        var prefix = plaintextKey.Substring(0, Math.Min(plaintextKey.Length, ApiKeyConsts.DisplayPrefixLength + 4));

        // 4. Compute SHA-256 hash of the complete plaintext key
        var hash = ComputeHash(plaintextKey);

        return (plaintextKey, prefix, hash);
    }

    public string ComputeHash(string plaintextKey)
    {
        if (string.IsNullOrWhiteSpace(plaintextKey))
            throw new ArgumentException("Plaintext key cannot be empty.", nameof(plaintextKey));

        var keyBytes = Encoding.UTF8.GetBytes(plaintextKey.Trim());
        var hashBytes = SHA256.HashData(keyBytes);

        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }
}
