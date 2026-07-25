using AnkiBridge.Infrastructure.Persistence.Abstractions;
using AnkiBridge.Infrastructure.Persistence.DatabaseContext;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Diagnostics;

namespace AnkiBridge.MigrationService;

public sealed class Worker(
    IServiceProvider serviceProvider,
    IHostApplicationLifetime hostApplicationLifetime,
    IOptions<MigrationServiceOptions> options,
    ILogger<Worker> logger)
    : BackgroundService
{
    public const string ActivitySourceName = "Migrations";
    private static readonly ActivitySource s_activitySource = new(ActivitySourceName);

    private readonly MigrationServiceOptions _options = options.Value;

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        using var activity = s_activitySource.StartActivity("Migrating database", ActivityKind.Client);

        try
        {
            if (!_options.RunMigrations && !_options.RunSeeders)
                return;

            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var strategy = context.Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(async () =>
            {
                if (_options.RunMigrations)
                {
                    logger.LogInformation("Starting database migrations...");
                    await context.Database.MigrateAsync(cancellationToken);
                    logger.LogInformation("Database migrations completed successfully.");
                }

                if (_options.RunSeeders)
                {
                    var seeders = scope.ServiceProvider
                       .GetServices<IDbSeeder>()
                       .OrderBy(s => s.Order)
                       .ToList();

                    if (seeders.Count == 0)
                    {
                        logger.LogInformation("No database seeders registered.");
                        return;
                    }

                    logger.LogInformation("Starting execution of {Count} database seeder(s)...", seeders.Count);

                    await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);

                    foreach (var seeder in seeders)
                    {
                        logger.LogInformation("Running seeder: {SeederName}", seeder.GetType().Name);
                        await seeder.SeedAsync(context, cancellationToken);
                        await context.SaveChangesAsync(cancellationToken);
                        context.ChangeTracker.Clear();
                    }

                    await transaction.CommitAsync(cancellationToken);
                    logger.LogInformation("All database seeders completed successfully.");
                }
            });
        }
        catch (Exception ex)
        {
            activity?.AddException(ex);
            logger.LogError(ex, "An error occurred during the database migration/seeding process.");
            Environment.ExitCode = 1;
            throw;
        }
        finally
        {
            hostApplicationLifetime.StopApplication();
        }
    }
}
