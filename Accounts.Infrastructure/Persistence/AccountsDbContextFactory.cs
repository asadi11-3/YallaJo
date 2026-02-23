using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;

namespace Accounts.Infrastructure.Persistence;


internal sealed class AccountsDbContextFactory : IDesignTimeDbContextFactory<AccountsDbContext>
{
    public AccountsDbContext CreateDbContext(string[] args)
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

        var connectionString = configuration.GetConnectionString("AccountsConnection")
            ?? throw new InvalidOperationException(
                "Connection string 'AccountsConnection' is not configured in YallaJo.Api/appsettings.json.");

        var options = new DbContextOptionsBuilder<AccountsDbContext>()
            .UseSqlServer(connectionString, sql =>
                sql.MigrationsHistoryTable("__EFMigrationsHistory", "accounts"))
            .Options;

        return new AccountsDbContext(options);
    }

    
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
            "dotnet ef migrations add <Name> -p Accounts.Infrastructure -s YallaJo.Api");
    }
}
