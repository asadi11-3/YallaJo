using Analytics.Application.Interfaces;
using Analytics.Contracts.Authorization;
using Analytics.Contracts.Services;
using Analytics.Domain.Repositories;
using Analytics.Infrastructure.Persistence;
using Analytics.Infrastructure.Persistence.Seeding;
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
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));
        services.AddScoped<IOutboxProcessor, OutboxProcessor<AnalyticsDbContext>>();
        services.AddScoped<IOutboxCleaner, OutboxCleaner<AnalyticsDbContext>>();

        // Repositories (5 + outbox writer)
        services.AddScoped<IPopularityScoreRepository, PopularityScoreRepository>();
        services.AddScoped<IUserInteractionRepository, UserInteractionRepository>();
        services.AddScoped<IAuditLogRepository, AuditLogRepository>();
        services.AddScoped<IRecommendationCacheRepository, RecommendationCacheRepository>();
        services.AddScoped<IUserPreferenceRepository, UserPreferenceRepository>();
        services.AddScoped<IAnalyticsOutboxWriter, AnalyticsOutboxWriter>();

        // Module-specific abstractions (noop stubs)
        services.AddScoped<IClientContextProvider, NoopClientContextProvider>();

        // Permission catalog (singleton)
        services.AddSingleton<IPermissionCatalog, AnalyticsPermissionCatalog>();

        return services;
    }
}
