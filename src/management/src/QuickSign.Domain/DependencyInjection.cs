using Microsoft.Extensions.DependencyInjection;
using QuickSign.Domain.Certificates;
using QuickSign.Domain.Data;

namespace QuickSign.Domain;

/// <summary>
/// Extension methods for registering Domain layer services in the DI container.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddDomainServices(this IServiceCollection services)
    {
        services.AddScoped<ICertificateManager, CertificateManager>();
        services.AddScoped<QuickSignDbMigrationService>();

        return services;
    }
}
