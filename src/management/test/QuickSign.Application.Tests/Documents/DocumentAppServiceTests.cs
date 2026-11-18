using AutoMapper;
using FluentAssertions;
using Moq;
using QuickSign.Application.Contracts.DTOs.Documents;
using QuickSign.Application.Contracts.Infrastructure;
using QuickSign.Application.Services;
using QuickSign.Domain.Documents;
using QuickSign.Domain.Exceptions;
using QuickSign.Domain.Repositories;
using QuickSign.Domain.Shared;
using QuickSign.Domain.Shared.Documents;
using Xunit;

namespace QuickSign.Application.Tests.Documents;

public class DocumentAppServiceTests
{
    private readonly Mock<ISignedDocumentRepository> _documentRepoMock = new();
    private readonly Mock<ICurrentUserService> _currentUserServiceMock = new();
    private readonly Mock<IMapper> _mapperMock = new();

    private const string CurrentUserId = "usr_01HXYZ987654321";

    public DocumentAppServiceTests()
    {
        _currentUserServiceMock.Setup(s => s.GetRequiredUserId()).Returns(CurrentUserId);
    }

    private DocumentAppService CreateService()
    {
        return new DocumentAppService(
            _documentRepoMock.Object,
            _currentUserServiceMock.Object,
            _mapperMock.Object
        );
    }

    [Fact]
    public async Task GetByIdAsync_WhenDocumentNotFound_ShouldThrowBusinessExceptionWithDocumentNotFound()
    {
        // Arrange
        var docId = Guid.NewGuid();
        _documentRepoMock.Setup(r => r.GetByIdAsync(docId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((SignedDocument?)null);

        var service = CreateService();

        // Act
        var act = async () => await service.GetByIdAsync(docId);

        // Assert
        var ex = await act.Should().ThrowAsync<BusinessException>();
        ex.Which.ErrorCode.Should().Be(QuickSignDomainErrorCodes.DocumentNotFound);
        ex.Which.HttpStatus.Should().Be(404);
    }

    [Fact]
    public async Task GetByCodeAsync_WhenDocumentNotFound_ShouldThrowBusinessExceptionWithDocumentNotFound()
    {
        // Arrange
        const string nonExistentCode = "DOC-NOT-FOUND-9999";
        _documentRepoMock.Setup(r => r.GetByCodeAsync(nonExistentCode, It.IsAny<CancellationToken>()))
            .ReturnsAsync((SignedDocument?)null);

        var service = CreateService();

        // Act
        var act = async () => await service.GetByCodeAsync(nonExistentCode);

        // Assert
        var ex = await act.Should().ThrowAsync<BusinessException>();
        ex.Which.ErrorCode.Should().Be(QuickSignDomainErrorCodes.DocumentNotFound);
        ex.Which.HttpStatus.Should().Be(404);
    }

    [Fact]
    public async Task GetByIdAsync_WhenUserIsNotOwnerAndNotAdmin_ShouldThrowForbiddenAccess()
    {
        // Arrange
        var docId = Guid.NewGuid();
        const string otherUserId = "usr_OTHER_USER";

        var doc = new SignedDocument(
            id: docId,
            documentCode: new DocumentCode("DOC-2026-TEST01"),
            userId: otherUserId,
            certificateId: Guid.NewGuid(),
            apiKeyId: null,
            docType: DocumentType.Pdf,
            documentName: "Contract.pdf",
            originalHash: new Sha256Hash("e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855"),
            signedHash: new Sha256Hash("a1b2c3d4e5f60718293a4b5c6d7e8f90123456789abcdef0123456789abcdef0"),
            signatureValue: "SAMPLE_SIG"
        );

        _currentUserServiceMock.Setup(s => s.IsInRole("Admin")).Returns(false);
        _documentRepoMock.Setup(r => r.GetByIdAsync(docId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(doc);

        var service = CreateService();

        // Act
        var act = async () => await service.GetByIdAsync(docId);

        // Assert
        var ex = await act.Should().ThrowAsync<BusinessException>();
        ex.Which.ErrorCode.Should().Be(QuickSignDomainErrorCodes.ForbiddenAccess);
        ex.Which.HttpStatus.Should().Be(403);
    }

    [Fact]
    public async Task GetByIdAsync_WhenOwnerAccesses_ShouldReturnDocumentDto()
    {
        // Arrange
        var docId = Guid.NewGuid();

        var doc = new SignedDocument(
            id: docId,
            documentCode: new DocumentCode("DOC-2026-TEST02"),
            userId: CurrentUserId,
            certificateId: Guid.NewGuid(),
            apiKeyId: null,
            docType: DocumentType.Pdf,
            documentName: "MyInvoice.pdf",
            originalHash: new Sha256Hash("e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855"),
            signedHash: new Sha256Hash("a1b2c3d4e5f60718293a4b5c6d7e8f90123456789abcdef0123456789abcdef0"),
            signatureValue: "SAMPLE_SIG"
        );

        _documentRepoMock.Setup(r => r.GetByIdAsync(docId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(doc);

        var expectedDto = new SignedDocumentDto
        {
            Id = doc.Id,
            DocumentCode = doc.DocumentCode.Value,
            UserId = doc.UserId,
            DocumentName = doc.DocumentName
        };

        _mapperMock.Setup(m => m.Map<SignedDocumentDto>(doc)).Returns(expectedDto);

        var service = CreateService();

        // Act
        var result = await service.GetByIdAsync(docId);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(docId);
        result.DocumentCode.Should().Be("DOC-2026-TEST02");
    }
}
