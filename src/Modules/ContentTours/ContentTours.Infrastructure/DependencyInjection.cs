using ContentTours.Application.Interfaces;
using ContentTours.Contracts.Authorization;
using ContentTours.Contracts.Tours;
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

        // Single canonical UoW for ContentTours.
        // ContentToursUnitOfWork wraps IUnitOfWork<ContentToursDbContext> which
        // dispatches IAggregateRoot domain events before SaveChangesAsync.
        // There is intentionally one SaveChanges path through the module so that
        // future domain events on any aggregate (Tour, TourPackage, ...) cannot
        // be silently dropped via a non-dispatching code path.
        services.AddScoped<IUnitOfWork<ContentToursDbContext>, UnitOfWork<ContentToursDbContext>>();
        services.AddScoped<IContentToursUnitOfWork, ContentToursUnitOfWork>();
        services.AddScoped<IContentToursInboxStore, ContentToursInboxStore>();
        services.AddScoped<IModuleDbInitializer, ContentToursDbInitializer>();
        // FE-2D development-only smoke data (Development-gated by UseDataSeedingAsync).
        services.AddScoped<IModuleDbInitializer, Fe2dSmokeTourGuideSeeder>();
        // DEV-SEED-B1: Development/QA-only seeder (guarded internally by IHostEnvironment.IsDevelopment()).
        services.AddScoped<IModuleDbInitializer, DevToursSeeder>();
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));
        services.AddScoped<IOutboxProcessor, OutboxProcessor<ContentToursDbContext>>();
        services.AddScoped<IOutboxCleaner, OutboxCleaner<ContentToursDbContext>>();

        // ── Repositories ──────────────────────────────────────────────────────
        services.AddScoped<ITourRepository, TourRepository>();
        services.AddScoped<ITourScheduleRepository, TourScheduleRepository>();
        services.AddScoped<ITourPricingTierRepository, TourPricingTierRepository>();
        services.AddScoped<ITourPricingTierTranslationRepository, TourPricingTierTranslationRepository>();
        services.AddScoped<ITourGuideRepository, TourGuideRepository>();
        services.AddScoped<ITourTourGuideRepository, TourTourGuideRepository>();
        services.AddScoped<ITourWaypointRepository, TourWaypointRepository>();
        services.AddScoped<ITourPackageRepository, TourPackageRepository>();
        services.AddScoped<IGuideApplicationRepository, GuideApplicationRepository>();
        services.AddScoped<ITourProposalRepository, TourProposalRepository>();
        services.AddScoped<IGuideTourOfferingRepository, GuideTourOfferingRepository>();
        services.AddScoped<IGuideScheduleRepository, GuideScheduleRepository>();
        services.AddScoped<IGuidePricingTierRepository, GuidePricingTierRepository>();

        // ── Outbox writer (Application layer uses this to avoid DbContext dependency) ──
        services.AddScoped<IContentToursOutboxWriter, ContentToursOutboxWriter>();

        // ── One-time backfill: re-emit enriched TourApproved events for existing approved tours ──
        services.AddScoped<ITourSnapshotBackfillService, TourSnapshotBackfillService>();

        // ── Cross-module stubs (replaced by real implementations in other modules) ──
        services.AddScoped<IScheduleBookingCountService, NoOpScheduleBookingCountService>();

        services.AddScoped<ITourCapacityService, NoOpTourCapacityService>();
        services.AddScoped<ITourExistenceService,TourExistenceService>();
        services.AddScoped<IGuideScheduleReader, GuideScheduleReader>();
        services.AddScoped<IGuideAvailabilityBlockRepository, GuideAvailabilityBlockRepository>();

        // Phase C cross-module stubs — placeholder bindings until Security and
        // Accounts modules ship their canonical implementations. The real
        // adapters will replace these bindings via their own DI registrations
        // in Program.cs / module-specific extension methods.
        services.AddScoped<IUserRoleChecker, NoOpUserRoleChecker>();

        // ── Cross-module read-only services ──────────────────────────────────
        // Owned & implemented here so consumers (ContentCore, etc.) depend only on
        // ContentTours.Contracts and never on the Tours schema directly.
        services.AddScoped<ITourOwnershipService, TourOwnershipService>();

        // ── Permission catalog (discovered by PermissionSeeder) ───────────────
        services.AddSingleton<IPermissionCatalog, ContentToursPermissionCatalog>();

        return services;
    }
}

