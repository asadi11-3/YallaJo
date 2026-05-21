using Analytics.Application.Interfaces;
using Analytics.Application.Interfaces.Repositories;
using Analytics.Contracts.Authorization;
using Analytics.Contracts.Services;
using IAnalyticsOutboxWriter = Analytics.Domain.Repositories.IAnalyticsOutboxWriter;
using Analytics.Infrastructure.BackgroundServices;
using Analytics.Infrastructure.Persistence;
using Analytics.Infrastructure.Persistence.Seeding;
using Analytics.Infrastructure.Queues;
using Analytics.Infrastructure.Repositories;
using Analytics.Infrastructure.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using YallaJo.SharedKernel.Application.Authorization;
using YallaJo.SharedKernel.Infrastructure.BackgroundJobs;
using YallaJo.SharedKernel.Infrastructure.Data;
using YallaJo.SharedKernel.Infrastructure.Outbox;

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
        services.AddScoped<IAnalyticsUnitOfWork, AnalyticsUnitOfWork>();
        services.AddScoped<IModuleDbInitializer, AnalyticsDbInitializer>();
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly);
            cfg.RegisterServicesFromAssembly(typeof(Analytics.Application.DependencyInjection).Assembly);
        });
        services.AddScoped<IOutboxProcessor, OutboxProcessor<AnalyticsDbContext>>();
        services.AddScoped<IOutboxCleaner, OutboxCleaner<AnalyticsDbContext>>();
        services.AddScoped<IAnalyticsInboxStore, AnalyticsInboxStore>();

        // Repositories
        services.AddScoped<IPopularityScoreRepository, PopularityScoreRepository>();
        services.AddScoped<IUserInteractionRepository, UserInteractionRepository>();
        services.AddScoped<IAuditLogRepository, AuditLogRepository>();
        services.AddScoped<IDashboardCacheRepository, DashboardCacheRepository>();
        services.AddScoped<IEntityPopularitySnapshotRepository, EntityPopularitySnapshotRepository>();
        services.AddScoped<IAnalyticsOutboxWriter, AnalyticsOutboxWriter>();
        services.AddScoped<IAuditLogRedactor, AuditLogRedactor>();
        services.AddScoped<IAnalyticsDashboardReader, AnalyticsDashboardReader>();
        services.AddSingleton<IInteractionIngestQueue, InteractionIngestQueue>();
        services.Configure<PopularityScoreCalculationOptions>(options =>
        {
            var section = configuration.GetSection(PopularityScoreCalculationOptions.SectionName);
            if (bool.TryParse(section[nameof(PopularityScoreCalculationOptions.Enabled)], out var enabled)) options.Enabled = enabled;
            if (TimeSpan.TryParse(section[nameof(PopularityScoreCalculationOptions.Interval)], out var interval)) options.Interval = interval;
            if (TimeSpan.TryParse(section[nameof(PopularityScoreCalculationOptions.InitialDelay)], out var initialDelay)) options.InitialDelay = initialDelay;
            if (int.TryParse(section[nameof(PopularityScoreCalculationOptions.BatchSize)], out var batchSize)) options.BatchSize = batchSize;
        });
        services.AddHostedService<InteractionIngestDrainService>();
        services.AddHostedService<PopularityScoreCalculationService>();

        // Module-specific abstractions (noop stubs)
        services.AddScoped<IClientContextProvider, NoopClientContextProvider>();

        // Permission catalog (singleton)
        services.AddSingleton<IPermissionCatalog, AnalyticsPermissionCatalog>();

        return services;
    }
}
