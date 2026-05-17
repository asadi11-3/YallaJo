using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Social.Application.Interfaces;
using Social.Contracts.Authorization;
using Social.Contracts.Services;
using Social.Domain.Repositories;
using Social.Infrastructure.Persistence;
using Social.Infrastructure.Persistence.Seeding;
using Social.Infrastructure.Repositories;
using Social.Infrastructure.Services;
using YallaJo.SharedKernel.Application.Authorization;
using YallaJo.SharedKernel.Infrastructure.Data;
using YallaJo.SharedKernel.Infrastructure.Outbox;
using YallaJo.SharedKernel.Infrastructure.BackgroundJobs;

namespace Social.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddSocialInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");

        services.AddDbContext<SocialDbContext>(options =>
            options.UseSqlServer(
                connectionString,
                sql =>
                {
                    sql.MigrationsHistoryTable("__EFMigrationsHistory", "social");
                    sql.EnableRetryOnFailure(3);
                }));

        services.AddScoped<IUnitOfWork<SocialDbContext>, UnitOfWork<SocialDbContext>>();
        services.AddScoped<IModuleDbInitializer, SocialDbInitializer>();
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));
        services.AddScoped<IOutboxProcessor, OutboxProcessor<SocialDbContext>>();
        services.AddScoped<IOutboxCleaner, OutboxCleaner<SocialDbContext>>();

        // ── Cross-module read-only services ──────────────────────────────────
        // Owned & implemented here so consumers (ContentCore, etc.) depend only on
        // Social.Contracts and never on the Social schema directly.
        services.AddScoped<IReviewOwnershipService, ReviewOwnershipService>();

        // UoW + Repositories
        services.AddScoped<ISocialUnitOfWork, SocialUnitOfWork>();
        services.AddScoped<IReviewRepository, ReviewRepository>();
        services.AddScoped<IFavoriteRepository, FavoriteRepository>();
        services.AddScoped<IReportRepository, ReportRepository>();
        services.AddScoped<ISocialOutboxWriter, SocialOutboxWriter>();

        // Content moderation stubs
        services.AddScoped<IProfanityFilter, NoopProfanityFilter>();
        services.AddScoped<INsfwClassifier, NoopNsfwClassifier>();

        // Permission catalog
        services.AddSingleton<IPermissionCatalog, SocialPermissionCatalog>();

        return services;
    }
}
