using AutoMapper;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using QuickSign.Application.Contracts.DTOs.Certificates;
using QuickSign.Application.Contracts.Infrastructure;
using QuickSign.Application.Services;
using QuickSign.Domain.Certificates;
using QuickSign.Domain.Exceptions;
using QuickSign.Domain.Repositories;
using QuickSign.Domain.Shared;
using QuickSign.Domain.Shared.Certificates;
using Xunit;

namespace QuickSign.Application.Tests.Certificates;

public class CertificateAppServiceTests
{
    private readonly Mock<ICertificateManager> _certificateManagerMock = new();
    private readonly Mock<ICertificateRepository> _certificateRepositoryMock = new();
    private readonly Mock<ICryptoEngineGrpcService> _cryptoEngineGrpcServiceMock = new();
    private readonly Mock<ICurrentUserService> _currentUserServiceMock = new();
    private readonly Mock<ICacheService> _cacheServiceMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<ILogger<CertificateAppService>> _loggerMock = new();
    private readonly Mock<IMapper> _mapperMock = new();

    private const string CurrentUserId = "usr_01HXYZ987654321";

    public CertificateAppServiceTests()
    {
        _currentUserServiceMock.Setup(s => s.GetRequiredUserId()).Returns(CurrentUserId);
    }

    private CertificateAppService CreateService()
    {
        return new CertificateAppService(
            _certificateManagerMock.Object,
            _certificateRepositoryMock.Object,
            _cryptoEngineGrpcServiceMock.Object,
            _currentUserServiceMock.Object,
            _cacheServiceMock.Object,
            _unitOfWorkMock.Object,
            _mapperMock.Object,
            _loggerMock.Object
        );
    }

    [Fact]
    public async Task IssueCertificateAsync_WhenDbFailsToSave_ShouldTriggerCompensatingTransactionToDeleteKeyFromVault()
    {
        // Arrange: TC-APP-01
        var request = new IssueCertificateRequestDto
        {
            SubjectName = "CN=Test Organization, O=Company, C=VN",
            Algorithm = KeyAlgorithmType.Rsa2048,
            ValidityDays = 365
        };

        const string vaultPath = "secret/data/users/usr_01HXYZ987654321/keys/cert-test";
        var generatedKeys = new GeneratedKeyMaterialDto
        {
            VaultKeyPath = vaultPath,
            PublicKeyPem = "-----BEGIN PUBLIC KEY-----\nMIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8A\n-----END PUBLIC KEY-----",
            SerialNumber = "4A7B8C9D"
        };

        _cryptoEngineGrpcServiceMock
            .Setup(c => c.GenerateCertificateKeyPairAsync(
                It.IsAny<Guid>(),
                CurrentUserId,
                request.SubjectName,
                request.Algorithm.ToString(),
                It.IsAny<DateTimeOffset>(),
                It.IsAny<DateTimeOffset>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(generatedKeys);

        var sampleCert = new Certificate(
            id: Guid.NewGuid(),
            userId: CurrentUserId,
            subjectName: request.SubjectName,
            serialNumber: generatedKeys.SerialNumber,
            algorithm: request.Algorithm,
            publicKeyPem: generatedKeys.PublicKeyPem,
            vaultKeyPath: vaultPath,
            expiresAt: DateTimeOffset.UtcNow.AddDays(365)
        );

        _certificateManagerMock
            .Setup(m => m.CreateAsync(
                It.IsAny<Guid>(),
                CurrentUserId,
                request.SubjectName,
                generatedKeys.SerialNumber,
                request.Algorithm,
                generatedKeys.PublicKeyPem,
                vaultPath,
                It.IsAny<DateTimeOffset>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(sampleCert);

        // Simulate PostgreSQL database commit failure
        _unitOfWorkMock
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("PostgreSQL connection lost."));

        var service = CreateService();

        // Act
        var act = async () => await service.IssueCertificateAsync(request);

        // Assert
        var ex = await act.Should().ThrowAsync<BusinessException>();
        ex.Which.ErrorCode.Should().Be(QuickSignDomainErrorCodes.DatabaseError);
        ex.Which.HttpStatus.Should().Be(500);

        // Verify Compensating Transaction was called to delete the key from Vault
        _cryptoEngineGrpcServiceMock.Verify(
            c => c.DeleteCertificateKeyPairAsync(vaultPath, CancellationToken.None),
            Times.Once,
            "Compensating transaction must be invoked to purge orphaned keys from Vault upon DB failure.");
    }

    [Fact]
    public async Task RevokeCertificateAsync_WhenNonAdminRevokesOtherUserCert_ShouldThrowUnauthorizedCertificateAccess()
    {
        // Arrange: TC-APP-02
        const string otherUserId = "usr_OTHER_USER_999";
        var certId = Guid.NewGuid();

        var certificate = new Certificate(
            id: certId,
            userId: otherUserId, // Owned by another user
            subjectName: "CN=Other User Key, C=VN",
            serialNumber: "SN-OTHER-01",
            algorithm: KeyAlgorithmType.Rsa2048,
            publicKeyPem: "-----BEGIN PUBLIC KEY-----\nKEY\n-----END PUBLIC KEY-----",
            vaultKeyPath: "secret/data/users/other/keys/k1",
            expiresAt: DateTimeOffset.UtcNow.AddDays(355)
        );

        _currentUserServiceMock.Setup(s => s.IsInRole("Admin")).Returns(false);
        _certificateRepositoryMock.Setup(r => r.GetByIdAsync(certId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(certificate);

        var request = new RevokeCertificateRequestDto
        {
            Reason = "Unauthorized revocation attempt",
            InvalidatedFrom = DateTimeOffset.UtcNow
        };

        var service = CreateService();

        // Act
        var act = async () => await service.RevokeCertificateAsync(certId, request);

        // Assert
        var ex = await act.Should().ThrowAsync<BusinessException>();
        ex.Which.ErrorCode.Should().Be(QuickSignDomainErrorCodes.UnauthorizedCertificateAccess);
        ex.Which.HttpStatus.Should().Be(403);

        // Ensure no DB changes or cache invalidation occurred
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        _cacheServiceMock.Verify(c => c.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RevokeCertificateAsync_WhenOwnerRevokes_ShouldSucceedAndInvalidateCache()
    {
        // Arrange
        var certId = Guid.NewGuid();
        var certificate = new Certificate(
            id: certId,
            userId: CurrentUserId, // Owned by current user
            subjectName: "CN=My Key, C=VN",
            serialNumber: "SN-MY-01",
            algorithm: KeyAlgorithmType.Rsa2048,
            publicKeyPem: "-----BEGIN PUBLIC KEY-----\nKEY\n-----END PUBLIC KEY-----",
            vaultKeyPath: "secret/data/users/my/keys/k1",
            expiresAt: DateTimeOffset.UtcNow.AddDays(360)
        );

        _certificateRepositoryMock.Setup(r => r.GetByIdAsync(certId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(certificate);

        var request = new RevokeCertificateRequestDto
        {
            Reason = "Key rotated",
            InvalidatedFrom = DateTimeOffset.UtcNow
        };

        var service = CreateService();

        // Act
        await service.RevokeCertificateAsync(certId, request);

        // Assert
        certificate.Status.Should().Be(CertificateStatusType.Revoked);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _cacheServiceMock.Verify(c => c.RemoveAsync($"cert:{certId}", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task IssueCertificateAsync_WhenValid_ShouldPersistAndReturnDto()
    {
        // Arrange
        var request = new IssueCertificateRequestDto
        {
            SubjectName = "CN=Valid Signer, C=VN",
            Algorithm = KeyAlgorithmType.Rsa2048,
            ValidityDays = 365
        };

        const string vaultPath = "secret/data/users/usr_01HXYZ987654321/keys/cert-valid";
        var generatedKeys = new GeneratedKeyMaterialDto
        {
            VaultKeyPath = vaultPath,
            PublicKeyPem = "-----BEGIN PUBLIC KEY-----\nMIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8A\n-----END PUBLIC KEY-----",
            SerialNumber = "VALID-SERIAL-123"
        };

        _cryptoEngineGrpcServiceMock
            .Setup(c => c.GenerateCertificateKeyPairAsync(
                It.IsAny<Guid>(),
                CurrentUserId,
                request.SubjectName,
                request.Algorithm.ToString(),
                It.IsAny<DateTimeOffset>(),
                It.IsAny<DateTimeOffset>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(generatedKeys);

        var sampleCert = new Certificate(
            id: Guid.NewGuid(),
            userId: CurrentUserId,
            subjectName: request.SubjectName,
            serialNumber: generatedKeys.SerialNumber,
            algorithm: request.Algorithm,
            publicKeyPem: generatedKeys.PublicKeyPem,
            vaultKeyPath: vaultPath,
            expiresAt: DateTimeOffset.UtcNow.AddDays(365)
        );

        _certificateManagerMock
            .Setup(m => m.CreateAsync(
                It.IsAny<Guid>(),
                CurrentUserId,
                request.SubjectName,
                generatedKeys.SerialNumber,
                request.Algorithm,
                generatedKeys.PublicKeyPem,
                vaultPath,
                It.IsAny<DateTimeOffset>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(sampleCert);

        var expectedDto = new CertificateResponseDto
        {
            Id = sampleCert.Id,
            UserId = sampleCert.UserId,
            SubjectName = sampleCert.SubjectName,
            SerialNumber = sampleCert.SerialNumber,
            Algorithm = sampleCert.Algorithm,
            PublicKeyPem = sampleCert.PublicKeyPem,
            Status = sampleCert.Status,
            ExpiresAt = sampleCert.ExpiresAt,
            CreatedAt = sampleCert.CreatedAt
        };

        _mapperMock.Setup(m => m.Map<CertificateResponseDto>(sampleCert)).Returns(expectedDto);

        var service = CreateService();

        // Act
        var result = await service.IssueCertificateAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(sampleCert.Id);
        result.SubjectName.Should().Be(request.SubjectName);
        _certificateRepositoryMock.Verify(r => r.InsertAsync(sampleCert, It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _cryptoEngineGrpcServiceMock.Verify(c => c.DeleteCertificateKeyPairAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
