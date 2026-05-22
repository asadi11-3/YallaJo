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
        services.AddScoped<ISuggestionBatchRepository, SuggestionBatchRepository>();
        services.AddScoped<IRecommendationCacheRepository, RecommendationCacheRepository>();
        services.AddScoped<IUserPreferenceRepository, UserPreferenceRepository>();
        services.AddScoped<IEntityAttributeSnapshotRepository, EntityAttributeSnapshotRepository>();
        services.AddScoped<IBoostPackageRepository, BoostPackageRepository>();
        services.AddScoped<IEditorialPinRepository, EditorialPinRepository>();
        services.AddScoped<IUserExcludedEntityRepository, UserExcludedEntityRepository>();
        services.AddScoped<ISeasonalityRuleRepository, SeasonalityRuleRepository>();
        services.AddScoped<IHolidayCalendarRepository, HolidayCalendarRepository>();
        services.AddScoped<ITripArcRepository, TripArcRepository>();
        services.AddScoped<IExperimentRepository, ExperimentRepository>();
        services.AddScoped<ISuggestionMetricRepository, SuggestionMetricRepository>();
        services.AddScoped<ISponsoredClickEventRepository, SponsoredClickEventRepository>();
        services.AddScoped<IGdprDeletionRequestRepository, GdprDeletionRequestRepository>();
        services.AddScoped<IExperimentVariantResolver, ExperimentVariantResolver>();
        services.AddScoped<Analytics.Application.Scoring.SponsoredAuctionService>();
        services.AddScoped<Analytics.Contracts.Services.IUserPreferenceLookupService, UserPreferenceLookupService>();
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
        services.Configure<SuggestionBatchRefreshOptions>(options =>
        {
            var section = configuration.GetSection(SuggestionBatchRefreshOptions.SectionName);
            if (int.TryParse(section[nameof(SuggestionBatchRefreshOptions.RefreshIntervalMinutes)], out var refreshIntervalMinutes)) options.RefreshIntervalMinutes = refreshIntervalMinutes;
            if (int.TryParse(section[nameof(SuggestionBatchRefreshOptions.StartupDelaySeconds)], out var startupDelaySeconds)) options.StartupDelaySeconds = startupDelaySeconds;
            if (int.TryParse(section[nameof(SuggestionBatchRefreshOptions.MaxBatchesPerCycle)], out var maxBatchesPerCycle)) options.MaxBatchesPerCycle = maxBatchesPerCycle;
        });
        services.AddHostedService<InteractionIngestDrainService>();
        services.AddHostedService<PopularityScoreCalculationService>();
        services.AddHostedService<SuggestionBatchRefreshJob>();
        services.AddHostedService<UserProfileUpdateJob>();
        services.AddHostedService<TripStageUpdateJob>();
        services.AddHostedService<GdprCleanupJob>();
        services.AddHostedService<EmailDigestBackgroundService>();
        services.AddHostedService<MetricsAggregationJob>();

        // Module-specific abstractions (noop stubs)
        services.AddScoped<IClientContextProvider, NoopClientContextProvider>();

        // Permission catalog (singleton)
        services.AddSingleton<IPermissionCatalog, AnalyticsPermissionCatalog>();

        return services;
    }
}
