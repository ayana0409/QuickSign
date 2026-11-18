using System.Security.Cryptography;
using System.Text;
using FluentAssertions;
using QuickSign.Domain.Shared.ApiKeys;
using QuickSign.Infrastructure.Security;
using Xunit;

namespace QuickSign.Application.Tests.Security;

public class ApiKeyCryptoServiceTests
{
    private readonly ApiKeyCryptoService _service = new();

    [Fact]
    public void GenerateApiKey_ShouldReturnValidKeyWithQsLivePrefixAndMatchingSha256Hash()
    {
        // Act: TC-INF-01
        var (plaintextKey, prefix, hash) = _service.GenerateApiKey();

        // Assert
        plaintextKey.Should().StartWith(ApiKeyConsts.LiveKeyPrefix);
        prefix.Should().StartWith(ApiKeyConsts.LiveKeyPrefix);
        
        // 32 bytes = 64 hex characters + "qs_live_" (8 chars) = 72 chars
        plaintextKey.Length.Should().Be(ApiKeyConsts.LiveKeyPrefix.Length + (ApiKeyConsts.SecretByteLength * 2));

        // SHA-256 digest is always 64 hexadecimal characters
        hash.Should().HaveLength(64);

        // Verify hash integrity matches independent computation
        var expectedHashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(plaintextKey));
        var expectedHash = Convert.ToHexString(expectedHashBytes).ToLowerInvariant();

        hash.Should().Be(expectedHash);
    }

    [Fact]
    public void ComputeHash_WithIdenticalInputs_ShouldProduceDeterministicOutput()
    {
        // Arrange
        const string sampleKey = "qs_live_0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef";

        // Act
        var hash1 = _service.ComputeHash(sampleKey);
        var hash2 = _service.ComputeHash(sampleKey);

        // Assert
        hash1.Should().Be(hash2);
        hash1.Should().HaveLength(64);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void ComputeHash_WithNullOrWhitespace_ShouldThrowArgumentException(string? invalidInput)
    {
        // Act
        var act = () => _service.ComputeHash(invalidInput!);

        // Assert
        act.Should().Throw<ArgumentException>();
    }
}
