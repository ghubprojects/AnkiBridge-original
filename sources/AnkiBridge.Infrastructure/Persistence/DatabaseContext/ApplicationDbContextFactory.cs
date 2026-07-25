using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace AnkiBridge.Infrastructure.Persistence.DatabaseContext;

public sealed class ApplicationDbContextFactory
    : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    private const string ConnectionStringName = "ankibridgedb";

    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationManager();
        var environment = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")
            ?? Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
            ?? "Development";

        if (environment.Equals("Development", StringComparison.OrdinalIgnoreCase))
            configuration.AddUserSecrets<ApplicationDbContextFactory>(optional: true);

        configuration.AddEnvironmentVariables();

        var connectionString = configuration.GetConnectionString(ConnectionStringName);
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                $"Connection string '{ConnectionStringName}' is required for EF tooling. "
                + $"Set ConnectionStrings:{ConnectionStringName} in User Secrets "
                + $"or ConnectionStrings__{ConnectionStringName} as an environment variable.");
        }

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        return new ApplicationDbContext(options);
    }
}
