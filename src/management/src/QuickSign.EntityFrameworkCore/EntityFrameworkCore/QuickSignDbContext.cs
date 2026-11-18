using Microsoft.EntityFrameworkCore;
using QuickSign.Domain.ApiKeys;
using QuickSign.Domain.Certificates;
using QuickSign.Domain.Documents;

namespace QuickSign.EntityFrameworkCore;

/// <summary>
/// Primary Entity Framework Core DbContext for QuickSign Management Service.
/// Encapsulates mapping and persistence across PostgreSQL tables.
/// </summary>
public class QuickSignDbContext : DbContext
{
    public DbSet<Certificate> Certificates => Set<Certificate>();
    public DbSet<ApiKey> ApiKeys => Set<ApiKey>();
    public DbSet<SignedDocument> SignedDocuments => Set<SignedDocument>();
    public DbSet<VerificationLog> VerificationLogs => Set<VerificationLog>();

    public QuickSignDbContext(DbContextOptions<QuickSignDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply all Fluent API configurations from this assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(QuickSignDbContext).Assembly);
    }
}
