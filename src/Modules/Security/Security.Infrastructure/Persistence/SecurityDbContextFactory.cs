using Microsoft.EntityFrameworkCore;
using YallaJo.SharedKernel.Infrastructure.Data;

namespace Security.Infrastructure.Persistence;


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
