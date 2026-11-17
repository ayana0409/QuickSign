using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using QuickSign.Domain.Data;

namespace QuickSign.DbMigrator;

/// <summary>
/// Hosted service that executes database schema migration and foundational data seeding on application startup,
/// then automatically shuts down the console application.
/// </summary>
public sealed class DbMigratorHostedService : IHostedService
{
    private readonly IHostApplicationLifetime _hostApplicationLifetime;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DbMigratorHostedService> _logger;

    public DbMigratorHostedService(
        IHostApplicationLifetime hostApplicationLifetime,
        IServiceProvider serviceProvider,
        ILogger<DbMigratorHostedService> logger)
    {
        _hostApplicationLifetime = hostApplicationLifetime;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("QuickSign DbMigrator starting execution...");

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var migrationService = scope.ServiceProvider.GetRequiredService<QuickSignDbMigrationService>();
            await migrationService.MigrateAsync(cancellationToken);

            _logger.LogInformation("Database migration and foundational data seeding completed successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "An error occurred during database migration execution.");
            throw;
        }
        finally
        {
            _hostApplicationLifetime.StopApplication();
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
