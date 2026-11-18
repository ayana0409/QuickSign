using FluentAssertions;
using Moq;
using QuickSign.Domain.Certificates;
using QuickSign.Domain.Exceptions;
using QuickSign.Domain.Repositories;
using QuickSign.Domain.Shared;
using QuickSign.Domain.Shared.Certificates;
using Xunit;

namespace QuickSign.Domain.Tests.Certificates;

public class CertificateManagerTests
{
    private readonly Mock<ICertificateRepository> _certRepoMock = new();
    private const string UserId = "usr_01HXYZ987654321";

    [Fact]
    public async Task CreateAsync_WhenUserHas5ActiveCertificates_ShouldThrowBusinessExceptionWithMaxActiveCertsReached()
    {
        // Arrange: TC-DOM-01
        _certRepoMock.Setup(r => r.CountActiveByUserIdAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(5);

        var manager = new CertificateManager(_certRepoMock.Object);

        // Act
        var act = async () => await manager.CreateAsync(
            id: Guid.NewGuid(),
            userId: UserId,
            subjectName: "CN=New 6th Certificate, C=VN",
            serialNumber: "SN-CERT-0006",
            algorithm: KeyAlgorithmType.Rsa2048,
            publicKeyPem: "-----BEGIN PUBLIC KEY-----\nSAMPLE\n-----END PUBLIC KEY-----",
            vaultKeyPath: "secret/data/users/abc/keys/k6",
            expiresAt: DateTimeOffset.UtcNow.AddYears(1)
        );

        // Assert
        var ex = await act.Should().ThrowAsync<BusinessException>();
        ex.Which.ErrorCode.Should().Be(QuickSignDomainErrorCodes.MaxActiveCertsReached);
        ex.Which.HttpStatus.Should().Be(400);
    }

    [Fact]
    public async Task CreateAsync_WhenUserHasLessThan5ActiveCertificates_ShouldCreateAndReturnCertificate()
    {
        // Arrange
        _certRepoMock.Setup(r => r.CountActiveByUserIdAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(4);

        var manager = new CertificateManager(_certRepoMock.Object);
        var id = Guid.NewGuid();
        var expiresAt = DateTimeOffset.UtcNow.AddYears(1);

        // Act
        var cert = await manager.CreateAsync(
            id: id,
            userId: UserId,
            subjectName: "CN=New 5th Certificate, C=VN",
            serialNumber: "SN-CERT-0005",
            algorithm: KeyAlgorithmType.EcdsaP256,
            publicKeyPem: "-----BEGIN PUBLIC KEY-----\nSAMPLE_ECDSA\n-----END PUBLIC KEY-----",
            vaultKeyPath: "secret/data/users/abc/keys/k5",
            expiresAt: expiresAt
        );

        // Assert
        cert.Should().NotBeNull();
        cert.Id.Should().Be(id);
        cert.UserId.Should().Be(UserId);
        cert.SubjectName.Should().Be("CN=New 5th Certificate, C=VN");
        cert.SerialNumber.Should().Be("SN-CERT-0005");
        cert.Algorithm.Should().Be(KeyAlgorithmType.EcdsaP256);
        cert.Status.Should().Be(CertificateStatusType.Active);
    }
}
