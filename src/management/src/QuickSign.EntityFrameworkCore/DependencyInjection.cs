using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using QuickSign.Domain.Data;
using QuickSign.Domain.Repositories;
using QuickSign.EntityFrameworkCore.Repositories;

namespace QuickSign.EntityFrameworkCore;

/// <summary>
/// Extension methods for registering EntityFrameworkCore layer services and PostgreSQL DbContext in the DI container.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddEntityFrameworkCoreServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Default");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("Connection string 'ConnectionStrings:Default' is required and cannot be empty.");
        }

        services.AddDbContext<QuickSignDbContext>(options =>
        {
            options.UseNpgsql(connectionString, b =>
            {
                b.MigrationsAssembly(typeof(QuickSignDbContext).Assembly.FullName);
            });
        });

        // Register Unit of Work and Schema Migrator
        services.AddScoped<IUnitOfWork, EfCoreUnitOfWork>();
        services.AddScoped<IQuickSignDbSchemaMigrator, EfCoreQuickSignDbSchemaMigrator>();

        // Register Repositories
        services.AddScoped<ICertificateRepository, EfCoreCertificateRepository>();
        services.AddScoped<IApiKeyRepository, EfCoreApiKeyRepository>();
        services.AddScoped<ISignedDocumentRepository, EfCoreSignedDocumentRepository>();

        return services;
    }
}
