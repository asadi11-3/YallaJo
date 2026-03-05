using Analytics.Infrastructure.Persistence;
using Analytics.Infrastructure.Persistence.Seeding;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using YallaJo.SharedKernel.Infrastructure.Data;
using YallaJo.SharedKernel.Infrastructure.BackgroundJobs;

namespace Analytics.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddAnalyticsInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");

        services.AddDbContext<AnalyticsDbContext>(options =>
            options.UseSqlServer(
                connectionString,
                sql =>
                {
                    sql.MigrationsHistoryTable("__EFMigrationsHistory", "analytics");
                    sql.EnableRetryOnFailure(3);
                }));

        services.AddScoped<IUnitOfWork<AnalyticsDbContext>, UnitOfWork<AnalyticsDbContext>>();
        services.AddScoped<IModuleDbInitializer, AnalyticsDbInitializer>();
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));
        services.AddScoped<IOutboxProcessor, OutboxProcessor<AnalyticsDbContext>>();

        return services;
    }
}
