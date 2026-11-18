using FluentAssertions;
using QuickSign.Domain.Certificates;
using QuickSign.Domain.Exceptions;
using QuickSign.Domain.Shared;
using QuickSign.Domain.Shared.Certificates;
using Xunit;

namespace QuickSign.Domain.Tests.Certificates;

public class CertificateTests
{
    private const string TestUserId = "usr_01HXYZ987654321";

    [Fact]
    public void Constructor_WithValidArguments_ShouldInitializePropertiesCorrectly()
    {
        // Arrange
        var id = Guid.NewGuid();
        var expiresAt = DateTimeOffset.UtcNow.AddYears(1);

        // Act
        var cert = new Certificate(
            id: id,
            userId: TestUserId,
            subjectName: "CN=QuickSign Test Signer, O=Enterprise, C=VN",
            serialNumber: "SN-2026-9999",
            algorithm: KeyAlgorithmType.Rsa2048,
            publicKeyPem: "-----BEGIN PUBLIC KEY-----\nMIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8A\n-----END PUBLIC KEY-----",
            vaultKeyPath: "secret/data/users/usr_01HXYZ987654321/keys/cert-1",
            expiresAt: expiresAt
        );

        // Assert
        cert.Id.Should().Be(id);
        cert.UserId.Should().Be(TestUserId);
        cert.SubjectName.Should().Be("CN=QuickSign Test Signer, O=Enterprise, C=VN");
        cert.SerialNumber.Should().Be("SN-2026-9999");
        cert.Algorithm.Should().Be(KeyAlgorithmType.Rsa2048);
        cert.VaultKeyPath.Should().Be("secret/data/users/usr_01HXYZ987654321/keys/cert-1");
        cert.Status.Should().Be(CertificateStatusType.Active);
        cert.Revocation.Should().BeNull();
        cert.ExpiresAt.Should().Be(expiresAt);
    }

    [Fact]
    public void Revoke_WithValidTimestamp_ShouldSetStatusToRevokedAndSetRevocationInfo()
    {
        // Arrange: TC-DOM-02
        var cert = new Certificate(
            id: Guid.NewGuid(),
            userId: TestUserId,
            subjectName: "CN=Revoke Target, C=VN",
            serialNumber: "SN-REVOKE-01",
            algorithm: KeyAlgorithmType.EcdsaP256,
            publicKeyPem: "-----BEGIN PUBLIC KEY-----\nECDSA_SAMPLE\n-----END PUBLIC KEY-----",
            vaultKeyPath: "secret/data/users/test/keys/cert-2",
            expiresAt: DateTimeOffset.UtcNow.AddYears(1)
        );

        var invalidatedFrom = cert.CreatedAt.AddSeconds(1);
        const string reason = "Private key compromised";

        // Act
        cert.Revoke(invalidatedFrom, reason);

        // Assert
        cert.Status.Should().Be(CertificateStatusType.Revoked);
        cert.Revocation.Should().NotBeNull();
        cert.Revocation!.InvalidatedFrom.Should().Be(invalidatedFrom);
        cert.Revocation.Reason.Should().Be(reason);
        cert.Revocation.RevokedAt.Should().BeOnOrAfter(cert.CreatedAt);
    }

    [Fact]
    public void Revoke_WhenInvalidatedFromEarlierThanCreatedAt_ShouldThrowBusinessExceptionWithInvalidRevocationTimestamp()
    {
        // Arrange: TC-DOM-03
        var cert = new Certificate(
            id: Guid.NewGuid(),
            userId: TestUserId,
            subjectName: "CN=Past Compromise Target, C=VN",
            serialNumber: "SN-REVOKE-02",
            algorithm: KeyAlgorithmType.Rsa2048,
            publicKeyPem: "-----BEGIN PUBLIC KEY-----\nRSA_SAMPLE\n-----END PUBLIC KEY-----",
            vaultKeyPath: "secret/data/users/test/keys/cert-3",
            expiresAt: DateTimeOffset.UtcNow.AddYears(1)
        );

        var invalidInvalidatedFrom = cert.CreatedAt.AddSeconds(-10); // Before CreatedAt

        // Act
        var act = () => cert.Revoke(invalidInvalidatedFrom, "Compromised before creation");

        // Assert
        act.Should().Throw<BusinessException>()
            .Where(ex => ex.ErrorCode == QuickSignDomainErrorCodes.InvalidRevocationTimestamp);
    }

    [Fact]
    public void Revoke_WhenCertificateAlreadyRevoked_ShouldThrowBusinessExceptionWithCertAlreadyRevoked()
    {
        // Arrange: TC-DOM-04
        var cert = new Certificate(
            id: Guid.NewGuid(),
            userId: TestUserId,
            subjectName: "CN=Double Revoke Target, C=VN",
            serialNumber: "SN-REVOKE-03",
            algorithm: KeyAlgorithmType.Rsa2048,
            publicKeyPem: "-----BEGIN PUBLIC KEY-----\nRSA_SAMPLE\n-----END PUBLIC KEY-----",
            vaultKeyPath: "secret/data/users/test/keys/cert-4",
            expiresAt: DateTimeOffset.UtcNow.AddYears(1)
        );

        cert.Revoke(cert.CreatedAt, "First revocation");

        // Act
        var act = () => cert.Revoke(cert.CreatedAt.AddMinutes(1), "Second revocation attempt");

        // Assert
        act.Should().Throw<BusinessException>()
            .Where(ex => ex.ErrorCode == QuickSignDomainErrorCodes.CertAlreadyRevoked);
    }

    [Fact]
    public void CheckAndMarkExpired_WhenCurrentTimeExceedsExpiresAt_ShouldMarkStatusAsExpired()
    {
        // Arrange
        var pastExpiresAt = DateTimeOffset.UtcNow.AddDays(-1);
        var cert = new Certificate(
            id: Guid.NewGuid(),
            userId: TestUserId,
            subjectName: "CN=Expired Target, C=VN",
            serialNumber: "SN-EXPIRED-01",
            algorithm: KeyAlgorithmType.Rsa2048,
            publicKeyPem: "-----BEGIN PUBLIC KEY-----\nRSA_SAMPLE\n-----END PUBLIC KEY-----",
            vaultKeyPath: "secret/data/users/test/keys/cert-5",
            expiresAt: pastExpiresAt
        );

        // Act
        cert.CheckAndMarkExpired();

        // Assert
        cert.Status.Should().Be(CertificateStatusType.Expired);
    }
}
