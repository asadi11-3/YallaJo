using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;

namespace Auth.Infrastructure.Persistence;

/// <summary>
/// Design-time factory used by EF Core CLI tools (migrations, scaffolding).
/// Reads the same appsettings files as the host so design-time and runtime
/// always use identical configuration.
///
/// Run migrations from the solution root:
///   dotnet ef migrations add Init -p Auth.Infrastructure -s YallaJo.Api
/// </summary>
internal sealed class AuthDbContextFactory : IDesignTimeDbContextFactory<AuthDbContext>
{
    public AuthDbContext CreateDbContext(string[] args)
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

        var connectionString = configuration.GetConnectionString("AuthConnection")
            ?? throw new InvalidOperationException(
                "Connection string 'AuthConnection' is not configured in YallaJo.Api/appsettings.json.");

        var options = new DbContextOptionsBuilder<AuthDbContext>()
            .UseSqlServer(connectionString, sql =>
                sql.MigrationsHistoryTable("__EFMigrationsHistory", "auth"))
            .Options;

        return new AuthDbContext(options);
    }

    /// <summary>
    /// Walks up from the current working directory looking for a sibling or ancestor
    /// directory named "YallaJo.Api". This is robust whether EF CLI is invoked from
    /// the project directory, solution root, or a build output directory.
    /// </summary>
    private static string ResolveApiProjectPath()
    {
        var current = new DirectoryInfo(Directory.GetCurrentDirectory());

        while (current != null)
        {
            // Look for YallaJo.Api as a sibling of the current directory
            var candidate = Path.Combine(current.FullName, "YallaJo.Api");
            if (Directory.Exists(candidate))
                return candidate;

            // Handle the case where the current directory IS YallaJo.Api
            if (current.Name.Equals("YallaJo.Api", StringComparison.OrdinalIgnoreCase))
                return current.FullName;

            current = current.Parent;
        }

        throw new DirectoryNotFoundException(
            "Could not locate 'YallaJo.Api' project directory. " +
            "Run EF CLI tools with the -s flag: " +
            "dotnet ef migrations add <Name> -p Auth.Infrastructure -s YallaJo.Api");
    }
}
