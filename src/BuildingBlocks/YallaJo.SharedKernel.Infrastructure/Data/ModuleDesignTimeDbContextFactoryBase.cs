using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace YallaJo.SharedKernel.Infrastructure.Data;


public abstract class ModuleDesignTimeDbContextFactoryBase<TContext>
    : IDesignTimeDbContextFactory<TContext>
    where TContext : DbContext
{
    /// <summary>
    /// Entry point called by EF Core tools (Add-Migration, Update-Database, etc.).
    /// Resolves DefaultConnection and delegates DbContext creation to the subclass.
    /// </summary>
    public TContext CreateDbContext(string[] args)
    {
        var connectionString = ResolveDefaultConnectionString();
        return CreateDbContext(connectionString);
    }

    /// <summary>
    /// Override to build and return the module-specific DbContext.
    /// The <paramref name="connectionString"/> is already validated and always
    /// comes from the "DefaultConnection" key — never hard-code or override it.
    /// </summary>
    /// <param name="connectionString">
    /// The DefaultConnection value from YallaJo.Api/appsettings.json.
    /// </param>
    protected abstract TContext CreateDbContext(string connectionString);

    // ── Private helpers ────────────────────────────────────────────────────────

    private static string ResolveDefaultConnectionString()
    {
        var environment =
            Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";

        var basePath = ResolveApiProjectPath();

        var configuration = new ConfigurationBuilder()
            .SetBasePath(basePath)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
            .AddJsonFile($"appsettings.{environment}.json", optional: true, reloadOnChange: false)
            .AddEnvironmentVariables()
            .Build();

        return configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "Connection string 'DefaultConnection' is not configured in " +
                "YallaJo.Api/appsettings.json. " +
                "All modules must share this single key — never add module-specific " +
                "connection string keys (AccountsConnection, AuthConnection, etc.).");
    }

    /// <summary>
    /// Walks up the directory tree from the current working directory until it
    /// finds the YallaJo.Api project folder. Works whether EF CLI is invoked
    /// from the solution root, a module directory, or a build output directory.
    /// </summary>
    private static string ResolveApiProjectPath()
    {
        var current = new DirectoryInfo(Directory.GetCurrentDirectory());

        while (current is not null)
        {
            // Case 1: YallaJo.Api is a sibling of (or inside) the current directory
            var candidate = Path.Combine(current.FullName, "YallaJo.Api");
            if (Directory.Exists(candidate))
                return candidate;

            // Case 2: the current directory IS YallaJo.Api
            if (current.Name.Equals("YallaJo.Api", StringComparison.OrdinalIgnoreCase))
                return current.FullName;

            current = current.Parent;
        }

        throw new DirectoryNotFoundException(
            "Could not locate the 'YallaJo.Api' project directory. " +
            "Run EF CLI from the solution root or specify the startup project explicitly:\n" +
            "  dotnet ef migrations add <Name> -p <Module>.Infrastructure -s YallaJo.Api\n" +
            "  Add-Migration <Name> -Project <Module>.Infrastructure -StartupProject YallaJo.Api");
    }
}
