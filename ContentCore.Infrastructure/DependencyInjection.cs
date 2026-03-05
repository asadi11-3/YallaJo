using ContentCore.Infrastructure.Persistence;
using ContentCore.Infrastructure.Persistence.Seeding;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using YallaJo.SharedKernel.Infrastructure.Data;
using YallaJo.SharedKernel.Infrastructure.BackgroundJobs;

namespace ContentCore.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddContentCoreInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");

        services.AddDbContext<ContentCoreDbContext>(options =>
            options.UseSqlServer(
                connectionString,
                sql =>
                {
                    sql.MigrationsHistoryTable("__EFMigrationsHistory", "content_core");
                    sql.EnableRetryOnFailure(3);
                }));

        services.AddScoped<IUnitOfWork<ContentCoreDbContext>, UnitOfWork<ContentCoreDbContext>>();
        services.AddScoped<IModuleDbInitializer, ContentCoreDbInitializer>();
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));
        services.AddScoped<IOutboxProcessor, OutboxProcessor<ContentCoreDbContext>>();

        return services;
    }
}
