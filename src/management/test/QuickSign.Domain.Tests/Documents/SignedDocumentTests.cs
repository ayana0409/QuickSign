using FluentAssertions;
using QuickSign.Domain.Documents;
using QuickSign.Domain.Shared.Documents;
using Xunit;

namespace QuickSign.Domain.Tests.Documents;

public class SignedDocumentTests
{
    private const string TestUserId = "usr_01HXYZ987654321";
    private readonly Guid _certId = Guid.NewGuid();

    [Fact]
    public void Constructor_WithValidArguments_ShouldInitializePropertiesCorrectly()
    {
        // Arrange
        var id = Guid.NewGuid();
        var origHash = new Sha256Hash("e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855");
        var signedHash = new Sha256Hash("a1b2c3d4e5f60718293a4b5c6d7e8f90123456789abcdef0123456789abcdef0");
        var docCode = new DocumentCode("DOC-2026-ABCD1234");

        // Act
        var doc = new SignedDocument(
            id: id,
            documentCode: docCode,
            userId: TestUserId,
            certificateId: _certId,
            apiKeyId: null,
            docType: DocumentType.Pdf,
            documentName: "Contract_Signed.pdf",
            originalHash: origHash,
            signedHash: signedHash,
            signatureValue: "MEQCIAz...",
            storagePath: "documents/2026/11/signed_doc.pdf"
        );

        // Assert
        doc.Id.Should().Be(id);
        doc.UserId.Should().Be(TestUserId);
        doc.CertificateId.Should().Be(_certId);
        doc.ApiKeyId.Should().BeNull();
        doc.DocType.Should().Be(DocumentType.Pdf);
        doc.DocumentCode.Value.Should().Be("DOC-2026-ABCD1234");
        doc.DocumentName.Should().Be("Contract_Signed.pdf");
        doc.OriginalHash.Value.Should().Be(origHash.Value);
        doc.SignedHash.Value.Should().Be(signedHash.Value);
        doc.SignatureValue.Should().Be("MEQCIAz...");
        doc.StoragePath.Should().Be("documents/2026/11/signed_doc.pdf");
        doc.TotalVerifyCount.Should().Be(0);
        doc.UniqueVerifyCount.Should().Be(0);
        doc.TamperedAlertCount.Should().Be(0);
        doc.LastVerifiedAt.Should().BeNull();
    }

    [Fact]
    public void IncrementMetrics_ShouldAtomicallyUpdateVerificationCounters()
    {
        // Arrange
        var doc = new SignedDocument(
            id: Guid.NewGuid(),
            documentCode: new DocumentCode("DOC-2026-XYZ98765"),
            userId: TestUserId,
            certificateId: _certId,
            apiKeyId: null,
            docType: DocumentType.Pdf,
            documentName: "Invoice.pdf",
            originalHash: new Sha256Hash("e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855"),
            signedHash: new Sha256Hash("a1b2c3d4e5f60718293a4b5c6d7e8f90123456789abcdef0123456789abcdef0"),
            signatureValue: "MEYCIQC..."
        );

        var verifyTime1 = DateTimeOffset.UtcNow;

        // Act 1: Unique valid verification
        doc.IncrementMetrics(isUnique: true, isTampered: false, verifyTime1);

        // Assert 1
        doc.TotalVerifyCount.Should().Be(1);
        doc.UniqueVerifyCount.Should().Be(1);
        doc.TamperedAlertCount.Should().Be(0);
        doc.LastVerifiedAt.Should().Be(verifyTime1);

        // Act 2: Repeated verification that detected tampered content
        var verifyTime2 = verifyTime1.AddMinutes(5);
        doc.IncrementMetrics(isUnique: false, isTampered: true, verifyTime2);

        // Assert 2
        doc.TotalVerifyCount.Should().Be(2);
        doc.UniqueVerifyCount.Should().Be(1); // not unique
        doc.TamperedAlertCount.Should().Be(1); // tampered
        doc.LastVerifiedAt.Should().Be(verifyTime2);
    }
}
