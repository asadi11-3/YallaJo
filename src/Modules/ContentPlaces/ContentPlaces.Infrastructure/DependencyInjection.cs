using ContentPlaces.Contracts.Authorization;
using ContentPlaces.Contracts.Places;
using ContentPlaces.Application.Interfaces;
using ContentPlaces.Infrastructure.Persistence;
using ContentPlaces.Infrastructure.Persistence.Seeding;
using ContentPlaces.Infrastructure.Services;
using YallaJo.SharedKernel.Application.Authorization;
using ContentPlaces.Domain.Repositories;
using ContentPlaces.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using YallaJo.SharedKernel.Infrastructure.Outbox;
using YallaJo.SharedKernel.Infrastructure.BackgroundJobs;
using YallaJo.SharedKernel.Application.Abstractions.Translation;
using YallaJo.SharedKernel.Infrastructure.Data;

namespace ContentPlaces.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddContentPlacesInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");

        services.AddDbContext<ContentPlacesDbContext>(options =>
            options.UseSqlServer(
                connectionString,
                sql =>
                {
                    sql.MigrationsHistoryTable("__EFMigrationsHistory", "content_places");
                    sql.EnableRetryOnFailure(3);
                }));

        // ── Repositories ─────────────────────────────────────────────────────
        services.AddScoped<IPlaceRepository, PlaceRepository>();
        services.AddScoped<IBusinessRepository, BusinessRepository>();
        services.AddScoped<IBusinessStaffRepository, BusinessStaffRepository>();
        services.AddScoped<IBusinessAmenityRepository, BusinessAmenityRepository>();
        services.AddScoped<IAccessibilityFeatureRepository, AccessibilityFeatureRepository>();
        services.AddScoped<IServiceItemRepository, ServiceItemRepository>();

        // ── Unit of Work & Infrastructure ────────────────────────────────────
        services.AddScoped<IUnitOfWork<ContentPlacesDbContext>, UnitOfWork<ContentPlacesDbContext>>();
        services.AddScoped<IContentPlacesUnitOfWork, ContentPlacesUnitOfWork>();
        services.AddScoped<IContentPlacesInboxStore, ContentPlacesInboxStore>();
        services.AddScoped<IContentPlacesOutboxWriter, ContentPlacesOutboxWriter>();
        services.AddScoped<IModuleDbInitializer, ContentPlacesDbInitializer>();
        // FE-2D development-only Place smoke data (Development-gated by UseDataSeedingAsync).
        services.AddScoped<IModuleDbInitializer, Fe2dSmokePlaceSeeder>();
        // DEV-SEED-B1: Development/QA-only seeder (guarded internally by IHostEnvironment.IsDevelopment()).
        services.AddScoped<IModuleDbInitializer, DevPlacesSeeder>();
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));
        services.AddScoped<IOutboxProcessor, OutboxProcessor<ContentPlacesDbContext>>();
        services.AddScoped<IOutboxCleaner, OutboxCleaner<ContentPlacesDbContext>>();

        // ── Translation Orchestrator (shared from ContentCore via DI) ─────────
        // IEntityTranslationOrchestrator is registered by ContentCore.Infrastructure.
        // ContentPlaces domain event handlers consume it; no separate registration needed here.

        // ── Cross-module read-only services ──────────────────────────────────
        // Owned & implemented here so consumers (ContentTours, etc.) depend only on
        // ContentPlaces.Contracts and never on the Places schema directly.
        services.AddScoped<IPlaceExistenceService, PlaceExistenceService>();
        services.AddScoped<IPlaceOwnershipService, PlaceOwnershipService>();

        // ── Permission catalog (discovered by Security.Infrastructure PermissionSeeder) ─
        services.AddSingleton<IPermissionCatalog, ContentPlacesPermissionCatalog>();

        return services;
    }
}
