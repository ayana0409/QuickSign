using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using QuickSign.Application.Contracts.Infrastructure;
using QuickSign.Infrastructure.Caching;
using QuickSign.Infrastructure.Cryptography;
using QuickSign.Infrastructure.Identity;
using QuickSign.Infrastructure.Security;
using StackExchange.Redis;

namespace QuickSign.Infrastructure;

/// <summary>
/// Extension methods for registering Infrastructure layer services (SSO Identity, gRPC, Redis, Security) in the DI container.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // 1. SSO Identity Options & HTTP Client
        services.Configure<SsoIdentityOptions>(
            configuration.GetSection(SsoIdentityOptions.SectionName));

        services.AddHttpClient<ISsoIdentityService, SsoIdentityService>();

        // 2. Current User Context
        services.AddScoped<ICurrentUserService, CurrentUserService>();

        // 3. gRPC Client Gateway to Go Crypto Engine
        services.AddSingleton<ICryptoEngineGrpcService, CryptoEngineGrpcService>();

        // 4. Redis ConnectionMultiplexer & Distributed Cache
        var redisConfig = configuration["Redis:Configuration"];
        if (string.IsNullOrWhiteSpace(redisConfig))
        {
            throw new InvalidOperationException("Configuration 'Redis:Configuration' is required and cannot be empty.");
        }
        services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(redisConfig));
        services.AddSingleton<ICacheService, RedisCacheService>();

        // 5. API Key Security & Cryptography
        services.AddSingleton<IApiKeyCryptoService, ApiKeyCryptoService>();

        return services;
    }
}
