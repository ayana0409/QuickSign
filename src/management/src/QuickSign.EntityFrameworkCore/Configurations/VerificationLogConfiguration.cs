using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using QuickSign.Domain.Documents;
using QuickSign.Domain.Shared.Documents;

namespace QuickSign.EntityFrameworkCore.Configurations;

/// <summary>
/// Entity Framework Core Fluent API mapping for VerificationLog child entity.
/// Maps strictly to table 'verification_logs' in PostgreSQL per system specification.
/// </summary>
public sealed class VerificationLogConfiguration : IEntityTypeConfiguration<VerificationLog>
{
    public void Configure(EntityTypeBuilder<VerificationLog> builder)
    {
        builder.ToTable("verification_logs");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasColumnName("id")
            .ValueGeneratedOnAdd();

        builder.Property(x => x.SignedDocumentId)
            .HasColumnName("signed_document_id")
            .IsRequired(false);
        builder.HasIndex(x => x.SignedDocumentId);

        builder.Property(x => x.SubmittedHash)
            .HasColumnName("submitted_hash")
            .HasConversion(
                v => v.Value,
                v => new Sha256Hash(v))
            .HasMaxLength(Sha256Hash.Length)
            .IsRequired();

        builder.Property(x => x.VerificationMethod)
            .HasColumnName("verification_method")
            .HasConversion(
                v => v.ToString().ToUpperInvariant(),
                // Strip underscores so SCREAMING_SNAKE_CASE values written by Go (e.g. WEB_UPLOAD) parse cleanly
                v => Enum.Parse<VerificationMethodType>(v.Replace("_", string.Empty), true))
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(x => x.ApiKeyId)
            .HasColumnName("api_key_id")
            .IsRequired(false);

        builder.Property(x => x.VerifierIp)
            .HasColumnName("verifier_ip")
            .HasMaxLength(45)
            .IsRequired();

        builder.Property(x => x.UserAgent)
            .HasColumnName("user_agent")
            .HasColumnType("text")
            .IsRequired(false);

        builder.Property(x => x.IsValid)
            .HasColumnName("is_valid")
            .IsRequired();

        builder.Property(x => x.ValidationStatus)
            .HasColumnName("validation_status")
            .HasConversion(
                v => v.ToString().ToUpperInvariant(),
                // Strip underscores so SCREAMING_SNAKE_CASE values written by Go (e.g. CODE_EXISTS_UNVERIFIED_CONTENT) parse cleanly
                v => Enum.Parse<ValidationStatusType>(v.Replace("_", string.Empty), true))
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.VerifiedAt)
            .HasColumnName("verified_at")
            .IsRequired();
        builder.HasIndex(x => x.VerifiedAt);
    }
}
