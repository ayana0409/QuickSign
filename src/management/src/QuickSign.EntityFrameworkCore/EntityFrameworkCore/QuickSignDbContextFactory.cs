using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace QuickSign.EntityFrameworkCore;

/// <summary>
/// Design-time DbContext factory enabling Entity Framework Core CLI migrations.
/// Reads the default connection string from environment variables or falls back to server configuration.
/// </summary>
public sealed class QuickSignDbContextFactory : IDesignTimeDbContextFactory<QuickSignDbContext>
{
    public QuickSignDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__Default")
            ?? throw new InvalidOperationException("Default connection string is missing from configuration.");

        var builder = new DbContextOptionsBuilder<QuickSignDbContext>();
        builder.UseNpgsql(connectionString, b =>
        {
            b.MigrationsAssembly(typeof(QuickSignDbContext).Assembly.FullName);
        });

        return new QuickSignDbContext(builder.Options);
    }
}
