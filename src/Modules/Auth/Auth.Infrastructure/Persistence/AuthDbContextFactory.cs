using Microsoft.EntityFrameworkCore;
using YallaJo.SharedKernel.Infrastructure.Data;

namespace Auth.Infrastructure.Persistence;

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
