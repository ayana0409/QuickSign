using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using QuickSign.Application.Contracts.Services;
using QuickSign.Application.Mappings;
using QuickSign.Application.Services;
using QuickSign.Application.Validators;

namespace QuickSign.Application;

/// <summary>
/// Extension methods for registering Application layer services, AutoMapper profiles, and validators in the DI container.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        // AutoMapper
        services.AddAutoMapper(cfg =>
        {
            cfg.AddProfile<QuickSignApplicationAutoMapperProfile>();
        });

        // FluentValidation
        services.AddValidatorsFromAssemblyContaining<IssueCertificateRequestValidator>();

        // Application Services
        services.AddScoped<IAuthAppService, AuthAppService>();
        services.AddScoped<ICertificateAppService, CertificateAppService>();
        services.AddScoped<IApiKeyAppService, ApiKeyAppService>();
        services.AddScoped<IDocumentAppService, DocumentAppService>();
        services.AddScoped<IStatisticsAppService, StatisticsAppService>();
        services.AddScoped<IAdminAppService, AdminAppService>();

        return services;
    }
}
