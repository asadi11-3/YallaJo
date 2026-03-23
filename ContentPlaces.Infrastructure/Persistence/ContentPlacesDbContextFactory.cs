using Microsoft.EntityFrameworkCore;
using YallaJo.SharedKernel.Infrastructure.Data;

namespace ContentPlaces.Infrastructure.Persistence;

internal sealed class ContentPlacesDbContextFactory
    : ModuleDesignTimeDbContextFactoryBase<ContentPlacesDbContext>
{
    protected override ContentPlacesDbContext CreateDbContext(string connectionString)
    {
        var options = new DbContextOptionsBuilder<ContentPlacesDbContext>()
            .UseSqlServer(connectionString, sql =>
            {
                sql.MigrationsHistoryTable("__EFMigrationsHistory", "content_places");
                sql.EnableRetryOnFailure(3);
            })
            .Options;

        return new ContentPlacesDbContext(options);
    }
}
