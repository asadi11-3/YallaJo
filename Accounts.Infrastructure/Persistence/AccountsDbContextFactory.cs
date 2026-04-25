using Microsoft.EntityFrameworkCore;
using YallaJo.SharedKernel.Infrastructure.Data;

namespace Accounts.Infrastructure.Persistence;

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
