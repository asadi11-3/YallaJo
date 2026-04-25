using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Tracking.Infrastructure.Persistence.Seeding;
using Tracking.Infrastructure.Persistence;
using YallaJo.SharedKernel.Infrastructure.Data;
using YallaJo.SharedKernel.Infrastructure.Outbox;
using YallaJo.SharedKernel.Infrastructure.BackgroundJobs;

namespace Tracking.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddTrackingInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");

        services.AddDbContext<TrackingDbContext>(options =>
            options.UseSqlServer(
                connectionString,
                sql =>
                {
                    sql.MigrationsHistoryTable("__EFMigrationsHistory", "tracking");
                    sql.EnableRetryOnFailure(3);
                }));

        services.AddScoped<IUnitOfWork<TrackingDbContext>, UnitOfWork<TrackingDbContext>>();
        services.AddScoped<IModuleDbInitializer, TrackingDbInitializer>();
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));
        services.AddScoped<IOutboxProcessor, OutboxProcessor<TrackingDbContext>>();
        services.AddScoped<IOutboxCleaner, OutboxCleaner<TrackingDbContext>>();

        return services;
    }
}
