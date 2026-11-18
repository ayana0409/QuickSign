using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using QuickSign.Domain.Certificates;
using QuickSign.Domain.Documents;
using QuickSign.Domain.Shared.Documents;

namespace QuickSign.EntityFrameworkCore.Configurations;

/// <summary>
/// Entity Framework Core Fluent API mapping for SignedDocument aggregate root.
/// Maps strictly to table 'signed_documents' in PostgreSQL per system specification.
/// </summary>
public sealed class SignedDocumentConfiguration : IEntityTypeConfiguration<SignedDocument>
{
    public void Configure(EntityTypeBuilder<SignedDocument> builder)
    {
        builder.ToTable("signed_documents");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasColumnName("id");

        builder.Property(x => x.DocumentCode)
            .HasColumnName("document_code")
            .HasConversion(
                v => v.Value,
                v => new DocumentCode(v))
            .HasMaxLength(50)
            .IsRequired();
        builder.HasIndex(x => x.DocumentCode)
            .IsUnique();

        builder.Property(x => x.UserId)
            .HasColumnName("user_id")
            .HasMaxLength(100)
            .IsRequired();
        builder.HasIndex(x => x.UserId);

        builder.Property(x => x.CertificateId)
            .HasColumnName("certificate_id")
            .IsRequired();
        builder.HasIndex(x => x.CertificateId);

        builder.HasOne<Certificate>()
            .WithMany()
            .HasForeignKey(x => x.CertificateId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(x => x.ApiKeyId)
            .HasColumnName("api_key_id")
            .IsRequired(false);
        builder.HasIndex(x => x.ApiKeyId);

        builder.Property(x => x.DocType)
            .HasColumnName("doc_type")
            .HasConversion(
                v => v.ToString().ToUpperInvariant(),
                v => Enum.Parse<DocumentType>(v, true))
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(x => x.DocumentName)
            .HasColumnName("document_name")
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(x => x.OriginalHash)
            .HasColumnName("original_hash")
            .HasConversion(
                v => v.Value,
                v => new Sha256Hash(v))
            .HasMaxLength(Sha256Hash.Length)
            .IsRequired();
        builder.HasIndex(x => x.OriginalHash);

        builder.Property(x => x.SignedHash)
            .HasColumnName("signed_hash")
            .HasConversion(
                v => v.Value,
                v => new Sha256Hash(v))
            .HasMaxLength(Sha256Hash.Length)
            .IsRequired();
        builder.HasIndex(x => x.SignedHash);

        builder.Property(x => x.SignatureValue)
            .HasColumnName("signature_value")
            .HasColumnType("text")
            .IsRequired();

        builder.Property(x => x.StoragePath)
            .HasColumnName("storage_path")
            .HasMaxLength(500)
            .IsRequired(false);

        builder.Property(x => x.TotalVerifyCount)
            .HasColumnName("total_verify_count")
            .HasDefaultValue(0)
            .IsRequired();

        builder.Property(x => x.UniqueVerifyCount)
            .HasColumnName("unique_verify_count")
            .HasDefaultValue(0)
            .IsRequired();

        builder.Property(x => x.TamperedAlertCount)
            .HasColumnName("tampered_alert_count")
            .HasDefaultValue(0)
            .IsRequired();

        builder.Property(x => x.LastVerifiedAt)
            .HasColumnName("last_verified_at")
            .IsRequired(false);

        builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        // Configure encapsulated child collection
        builder.HasMany(x => x.VerificationLogs)
            .WithOne()
            .HasForeignKey(x => x.SignedDocumentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(x => x.VerificationLogs)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
