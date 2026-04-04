using ContentPlaces.Application.Interfaces;
using ContentPlaces.Domain.Repositories;
using ContentPlaces.Infrastructure.Persistence;
using ContentPlaces.Infrastructure.Persistence.Seeding;
using ContentPlaces.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using YallaJo.SharedKernel.Application.Abstractions.Translation;
using YallaJo.SharedKernel.Infrastructure.Data;
using YallaJo.SharedKernel.Infrastructure.BackgroundJobs;

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

        // ── Unit of Work & Infrastructure ────────────────────────────────────
        services.AddScoped<IUnitOfWork<ContentPlacesDbContext>, UnitOfWork<ContentPlacesDbContext>>();
        services.AddScoped<IContentPlacesUnitOfWork, ContentPlacesUnitOfWork>();
        services.AddScoped<IContentPlacesInboxStore, ContentPlacesInboxStore>();
        services.AddScoped<IModuleDbInitializer, ContentPlacesDbInitializer>();
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));
        services.AddScoped<IOutboxProcessor, OutboxProcessor<ContentPlacesDbContext>>();

        // ── Translation Orchestrator (shared from ContentCore via DI) ─────────
        // IEntityTranslationOrchestrator is registered by ContentCore.Infrastructure.
        // ContentPlaces domain event handlers consume it; no separate registration needed here.

        return services;
    }
}
