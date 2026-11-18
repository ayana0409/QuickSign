using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using QuickSign.Domain.ApiKeys;
using QuickSign.Domain.Shared.ApiKeys;

namespace QuickSign.EntityFrameworkCore.Configurations;

/// <summary>
/// Entity Framework Core Fluent API mapping for ApiKey aggregate root.
/// Maps strictly to table 'api_keys' in PostgreSQL per system specification.
/// </summary>
public sealed class ApiKeyConfiguration : IEntityTypeConfiguration<ApiKey>
{
    public void Configure(EntityTypeBuilder<ApiKey> builder)
    {
        builder.ToTable("api_keys");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasColumnName("id");

        builder.Property(x => x.UserId)
            .HasColumnName("user_id")
            .HasMaxLength(100)
            .IsRequired();
        builder.HasIndex(x => x.UserId);

        builder.Property(x => x.Name)
            .HasColumnName("name")
            .HasMaxLength(ApiKeyConsts.MaxNameLength)
            .IsRequired();

        builder.Property(x => x.KeyPrefix)
            .HasColumnName("key_prefix")
            .HasMaxLength(ApiKeyConsts.MaxPrefixColumnLength)
            .IsRequired();

        builder.Property(x => x.KeyHash)
            .HasColumnName("key_hash")
            .HasConversion(
                v => v.Value,
                v => new ApiKeySecretHash(v))
            .HasMaxLength(ApiKeyConsts.Sha256HexLength)
            .IsRequired();
        builder.HasIndex(x => x.KeyHash)
            .IsUnique();

        builder.Property(x => x.Scopes)
            .HasColumnName("scopes")
            .HasMaxLength(ApiKeyConsts.MaxScopesLength)
            .IsRequired();

        builder.Property(x => x.IsActive)
            .HasColumnName("is_active")
            .HasDefaultValue(true)
            .IsRequired();

        builder.Property(x => x.LastUsedAt)
            .HasColumnName("last_used_at")
            .IsRequired(false);

        builder.Property(x => x.ExpiresAt)
            .HasColumnName("expires_at")
            .IsRequired(false);

        builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();
    }
}
