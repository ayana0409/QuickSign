using FluentAssertions;
using QuickSign.Domain.ApiKeys;
using Xunit;

namespace QuickSign.Domain.Tests.ApiKeys;

public class ApiKeyTests
{
    private const string TestUserId = "usr_01HXYZ987654321";

    [Fact]
    public void Constructor_WithValidArguments_ShouldInitializePropertiesCorrectly()
    {
        // Arrange & Act
        var id = Guid.NewGuid();
        var hash = new ApiKeySecretHash("e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855");
        var expiresAt = DateTimeOffset.UtcNow.AddMonths(6);

        var apiKey = new ApiKey(
            id: id,
            userId: TestUserId,
            name: "Production Backend Service",
            keyPrefix: "qs_live_12345678",
            keyHash: hash,
            scopes: "documents:sign,documents:verify",
            expiresAt: expiresAt
        );

        // Assert
        apiKey.Id.Should().Be(id);
        apiKey.UserId.Should().Be(TestUserId);
        apiKey.Name.Should().Be("Production Backend Service");
        apiKey.KeyPrefix.Should().Be("qs_live_12345678");
        apiKey.KeyHash.Value.Should().Be(hash.Value);
        apiKey.IsActive.Should().BeTrue();
        apiKey.LastUsedAt.Should().BeNull();
        apiKey.ExpiresAt.Should().Be(expiresAt);
        apiKey.HasScope("documents:sign").Should().BeTrue();
        apiKey.HasScope("documents:verify").Should().BeTrue();
        apiKey.HasScope("documents:delete").Should().BeFalse();
    }

    [Fact]
    public void Revoke_WhenActive_ShouldSetIsActiveToFalse()
    {
        // Arrange
        var hash = new ApiKeySecretHash("e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855");
        var apiKey = new ApiKey(
            id: Guid.NewGuid(),
            userId: TestUserId,
            name: "Key to deactivate",
            keyPrefix: "qs_live_abcdefgh",
            keyHash: hash,
            scopes: "documents:sign",
            expiresAt: DateTimeOffset.UtcNow.AddYears(1)
        );

        // Act
        apiKey.Revoke();

        // Assert
        apiKey.IsActive.Should().BeFalse();
    }

    [Fact]
    public void RecordUsage_ShouldUpdateLastUsedAt()
    {
        // Arrange
        var hash = new ApiKeySecretHash("e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855");
        var apiKey = new ApiKey(
            id: Guid.NewGuid(),
            userId: TestUserId,
            name: "Active key",
            keyPrefix: "qs_live_11223344",
            keyHash: hash,
            scopes: "documents:sign"
        );

        var usageTime = DateTimeOffset.UtcNow.AddMinutes(5);

        // Act
        apiKey.RecordUsage(usageTime);

        // Assert
        apiKey.LastUsedAt.Should().Be(usageTime);
    }
}
