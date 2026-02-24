using Microsoft.EntityFrameworkCore;
using YallaJo.SharedKernel.Infrastructure.Data;

namespace Accounts.Infrastructure.Persistence;

/// <summary>
/// Design-time factory for EF Core CLI tools (Add-Migration, Update-Database).
/// Inherits all configuration-loading logic from <see cref="ModuleDesignTimeDbContextFactoryBase{TContext}"/>,
/// which always reads "DefaultConnection" — the single shared database for all modules.
/// Schema isolation is maintained via the "accounts" default schema set in AccountsDbContext.
/// </summary>
internal sealed class AccountsDbContextFactory
    : ModuleDesignTimeDbContextFactoryBase<AccountsDbContext>
{
    protected override AccountsDbContext CreateDbContext(string connectionString)
    {
        var options = new DbContextOptionsBuilder<AccountsDbContext>()
            .UseSqlServer(connectionString, sql =>
                sql.MigrationsHistoryTable("__EFMigrationsHistory", "accounts"))
            .Options;

        return new AccountsDbContext(options);
    }
}
