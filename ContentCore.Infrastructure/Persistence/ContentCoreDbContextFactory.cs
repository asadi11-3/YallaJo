using Microsoft.EntityFrameworkCore;
using YallaJo.SharedKernel.Infrastructure.Data;

namespace ContentCore.Infrastructure.Persistence;

/// <summary>
/// Design-time factory for EF Core tooling (dotnet ef migrations add / update-database).
/// Reads the connection string from YallaJo.Api/appsettings.json — never hardcode here.
/// </summary>
public sealed class ContentCoreDbContextFactory
    : ModuleDesignTimeDbContextFactoryBase<ContentCoreDbContext>
{
    protected override ContentCoreDbContext CreateDbContext(string connectionString)
    {
        var options = new DbContextOptionsBuilder<ContentCoreDbContext>()
            .UseSqlServer(
                connectionString,
                sql => sql.MigrationsHistoryTable("__EFMigrationsHistory", "content_core"))
            .Options;

        return new ContentCoreDbContext(options);
    }
}
