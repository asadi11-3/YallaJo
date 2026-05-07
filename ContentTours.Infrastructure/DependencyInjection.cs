using ContentTours.Application.Interfaces;
using ContentTours.Contracts.Authorization;
using ContentTours.Domain.Repositories;
using ContentTours.Infrastructure.Persistence;
using ContentTours.Infrastructure.Persistence.Seeding;
using ContentTours.Infrastructure.Repositories;
using ContentTours.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using YallaJo.SharedKernel.Application.Abstractions.Data;
using YallaJo.SharedKernel.Application.Authorization;
using YallaJo.SharedKernel.Infrastructure.BackgroundJobs;
using YallaJo.SharedKernel.Infrastructure.Data;
using YallaJo.SharedKernel.Infrastructure.Outbox;

namespace ContentTours.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddContentToursInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");

        var isDevelopment = string.Equals(
            configuration["ASPNETCORE_ENVIRONMENT"] ?? Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"),
            "Development",
            StringComparison.OrdinalIgnoreCase);

        services.AddDbContext<ContentToursDbContext>(options =>
        {
            options.UseSqlServer(
                connectionString,
                sql =>
                {
                    sql.MigrationsHistoryTable("__EFMigrationsHistory", "content_tours");
                    sql.EnableRetryOnFailure(3);
                });

            if (isDevelopment)
            {
                options.ConfigureWarnings(warnings =>
                    warnings.Ignore(RelationalEventId.PendingModelChangesWarning));
            }
        });

        services.AddScoped<IDbContext>(sp =>
            sp.GetRequiredService<ContentToursDbContext>());

        services.AddScoped<IUnitOfWork<ContentToursDbContext>, UnitOfWork<ContentToursDbContext>>();
        services.AddScoped<IContentToursUnitOfWork, ContentToursUnitOfWork>();
        services.AddScoped<IContentToursEventUnitOfWork, ContentToursEventUnitOfWork>();
        services.AddScoped<IContentToursInboxStore, ContentToursInboxStore>();
        services.AddScoped<IModuleDbInitializer, ContentToursDbInitializer>();
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));
        services.AddScoped<IOutboxProcessor, OutboxProcessor<ContentToursDbContext>>();
        services.AddScoped<IOutboxCleaner, OutboxCleaner<ContentToursDbContext>>();

        // ── Repositories ──────────────────────────────────────────────────────
        services.AddScoped<ITourRepository, TourRepository>();
        services.AddScoped<ITourScheduleRepository, TourScheduleRepository>();
        services.AddScoped<ITourPricingTierRepository, TourPricingTierRepository>();
        services.AddScoped<ITourPricingTierTranslationRepository, TourPricingTierTranslationRepository>();
        services.AddScoped<ITourTourGuideRepository, TourTourGuideRepository>();
        services.AddScoped<ITourWaypointRepository, TourWaypointRepository>();
        services.AddScoped<ITourPackageRepository, TourPackageRepository>();

        // ── Outbox writer (Application layer uses this to avoid DbContext dependency) ──
        services.AddScoped<IContentToursOutboxWriter, ContentToursOutboxWriter>();

        // ── Cross-module stubs (replaced by real implementations in other modules) ──
        services.AddScoped<IScheduleBookingCountService, NoOpScheduleBookingCountService>();

        services.AddScoped<ITourCapacityService, NoOpTourCapacityService>();

        // Phase C cross-module stubs — placeholder bindings until Security and
        // Accounts modules ship their canonical implementations. The real
        // adapters will replace these bindings via their own DI registrations
        // in Program.cs / module-specific extension methods.
        services.AddScoped<IUserRoleChecker, NoOpUserRoleChecker>();
        services.AddScoped<IProfileLookupService, NoOpProfileLookupService>();

        // ── Permission catalog (discovered by PermissionSeeder) ───────────────
        services.AddSingleton<IPermissionCatalog, ContentToursPermissionCatalog>();

        return services;
    }
}
