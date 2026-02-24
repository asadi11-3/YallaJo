using Microsoft.EntityFrameworkCore;
using YallaJo.SharedKernel.Infrastructure.Data;

namespace Auth.Infrastructure.Persistence;

/// <summary>
/// Design-time factory for EF Core CLI tools (Add-Migration, Update-Database).
/// Inherits all configuration-loading logic from <see cref="ModuleDesignTimeDbContextFactoryBase{TContext}"/>,
/// which always reads "DefaultConnection" — the single shared database for all modules.
/// Schema isolation is maintained via the "auth" default schema set in AuthDbContext.
/// </summary>
internal sealed class AuthDbContextFactory
    : ModuleDesignTimeDbContextFactoryBase<AuthDbContext>
{
    protected override AuthDbContext CreateDbContext(string connectionString)
    {
        var options = new DbContextOptionsBuilder<AuthDbContext>()
            .UseSqlServer(connectionString, sql =>
                sql.MigrationsHistoryTable("__EFMigrationsHistory", "auth"))
            .Options;

        return new AuthDbContext(options);
    }
}
