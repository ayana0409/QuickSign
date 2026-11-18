using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using QuickSign.Domain.Certificates;
using QuickSign.Domain.Shared.Certificates;

namespace QuickSign.EntityFrameworkCore.Configurations;

/// <summary>
/// Entity Framework Core Fluent API mapping for Certificate aggregate root.
/// Maps strictly to table 'certificates' in PostgreSQL per system specification.
/// </summary>
public sealed class CertificateConfiguration : IEntityTypeConfiguration<Certificate>
{
    public void Configure(EntityTypeBuilder<Certificate> builder)
    {
        builder.ToTable("certificates");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasColumnName("id");

        builder.Property(x => x.UserId)
            .HasColumnName("user_id")
            .HasMaxLength(100)
            .IsRequired();
        builder.HasIndex(x => x.UserId);

        builder.Property(x => x.SubjectName)
            .HasColumnName("subject_name")
            .HasMaxLength(CertificateConsts.MaxSubjectNameLength)
            .IsRequired();

        builder.Property(x => x.SerialNumber)
            .HasColumnName("serial_number")
            .HasMaxLength(CertificateConsts.MaxSerialNumberLength)
            .IsRequired();
        builder.HasIndex(x => x.SerialNumber)
            .IsUnique();

        builder.Property(x => x.Algorithm)
            .HasColumnName("algorithm")
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.PublicKeyPem)
            .HasColumnName("public_key_pem")
            .HasColumnType("text")
            .IsRequired();

        builder.Property(x => x.VaultKeyPath)
            .HasColumnName("vault_key_path")
            .HasMaxLength(CertificateConsts.MaxVaultPathLength)
            .IsRequired();

        builder.Property(x => x.Status)
            .HasColumnName("status")
            .HasConversion(
                v => v.ToString().ToUpperInvariant(),
                v => Enum.Parse<CertificateStatusType>(v, true))
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(x => x.ExpiresAt)
            .HasColumnName("expires_at")
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        // Map Value Object RevocationInfo as owned properties in the same table
        builder.OwnsOne(x => x.Revocation, r =>
        {
            r.Property(p => p.RevokedAt)
                .HasColumnName("revoked_at");

            r.Property(p => p.InvalidatedFrom)
                .HasColumnName("invalidated_from");

            r.Property(p => p.Reason)
                .HasColumnName("revoked_reason")
                .HasMaxLength(255);
        });
    }
}
