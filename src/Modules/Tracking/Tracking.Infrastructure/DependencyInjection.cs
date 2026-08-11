using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Tracking.Application.Interfaces;
using Tracking.Contracts.Authorization;
using Tracking.Domain.Repositories;
using Tracking.Infrastructure.Persistence.Seeding;
using Tracking.Infrastructure.Persistence;
using Tracking.Infrastructure.Repositories;
using YallaJo.SharedKernel.Application.Authorization;
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
        services.AddScoped<ITrackingUnitOfWork, TrackingUnitOfWork>();
        services.AddScoped<IModuleDbInitializer, TrackingDbInitializer>();
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));
        services.AddScoped<IOutboxProcessor, OutboxProcessor<TrackingDbContext>>();
        services.AddScoped<IOutboxCleaner, OutboxCleaner<TrackingDbContext>>();

        // ── Repositories ─────────────────────────────────────────────────────
        services.AddScoped<ITrackingSessionRepository, TrackingSessionRepository>();

        // ── Permission catalog ───────────────────────────────────────────────
        services.AddSingleton<IPermissionCatalog, TrackingPermissionCatalog>();

        return services;
    }
}
