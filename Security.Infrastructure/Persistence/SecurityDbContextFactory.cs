using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Security.Infrastructure.Persistence;

internal sealed class SecurityDbContextFactory : IDesignTimeDbContextFactory<SecurityDbContext>
{
    public SecurityDbContext CreateDbContext(string[] args)
    {
        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
            ?? "Development";

        var basePath = ResolveApiProjectPath();

        var configuration = new ConfigurationBuilder()
            .SetBasePath(basePath)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
            .AddJsonFile($"appsettings.{environment}.json", optional: true, reloadOnChange: false)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = configuration.GetConnectionString("SecurityConnection")
            ?? throw new InvalidOperationException(
                "Connection string 'SecurityConnection' is not configured in YallaJo.Api/appsettings.json.");

        var options = new DbContextOptionsBuilder<SecurityDbContext>()
            .UseSqlServer(connectionString, sql =>
                sql.MigrationsHistoryTable("__EFMigrationsHistory", "security"))
            .Options;

        return new SecurityDbContext(options);
    }

    private static string ResolveApiProjectPath()
    {
        var current = new DirectoryInfo(Directory.GetCurrentDirectory());

        while (current != null)
        {
            var candidate = Path.Combine(current.FullName, "YallaJo.Api");
            if (Directory.Exists(candidate))
                return candidate;

            if (current.Name.Equals("YallaJo.Api", StringComparison.OrdinalIgnoreCase))
                return current.FullName;

            current = current.Parent;
        }

        throw new DirectoryNotFoundException(
            "Could not locate 'YallaJo.Api' project directory. " +
            "Run EF CLI tools with the -s flag: " +
            "dotnet ef migrations add <Name> -p Security.Infrastructure -s YallaJo.Api");
    }
}
