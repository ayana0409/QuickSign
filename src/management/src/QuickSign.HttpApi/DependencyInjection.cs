using Microsoft.Extensions.DependencyInjection;

namespace QuickSign.HttpApi;

/// <summary>
/// Extension methods for registering HttpApi layer controllers in the DI container.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddHttpApiControllers(this IServiceCollection services)
    {
        services.AddControllers()
            .AddApplicationPart(typeof(DependencyInjection).Assembly);

        return services;
    }
}
