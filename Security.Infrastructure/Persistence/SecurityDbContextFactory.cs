using Microsoft.EntityFrameworkCore;
using YallaJo.SharedKernel.Infrastructure.Data;

namespace Security.Infrastructure.Persistence;

/// <summary>
/// Design-time factory for EF Core CLI tools (Add-Migration, Update-Database).
/// Inherits all configuration-loading logic from <see cref="ModuleDesignTimeDbContextFactoryBase{TContext}"/>,
/// which always reads "DefaultConnection" — the single shared database for all modules.
/// Schema isolation is maintained via the "security" default schema set in SecurityDbContext.
/// </summary>
internal sealed class SecurityDbContextFactory
    : ModuleDesignTimeDbContextFactoryBase<SecurityDbContext>
{
    protected override SecurityDbContext CreateDbContext(string connectionString)
    {
        var options = new DbContextOptionsBuilder<SecurityDbContext>()
            .UseSqlServer(connectionString, sql =>
                sql.MigrationsHistoryTable("__EFMigrationsHistory", "security"))
            .Options;

        return new SecurityDbContext(options);
    }
}
