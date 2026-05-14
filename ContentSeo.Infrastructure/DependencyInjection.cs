using ContentSeo.Application.Interfaces;
using ContentSeo.Contracts.Authorization;
using ContentSeo.Infrastructure.BackgroundServices;
using ContentSeo.Infrastructure.Persistence;
using ContentSeo.Infrastructure.Persistence.Seeding;
using ContentSeo.Infrastructure.Repositories;
using ContentSeo.Infrastructure.Sitemap;
using ContentSeo.Infrastructure.Weather;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using YallaJo.SharedKernel.Application.Authorization;
using YallaJo.SharedKernel.Infrastructure.BackgroundJobs;
using YallaJo.SharedKernel.Infrastructure.Data;
using YallaJo.SharedKernel.Infrastructure.Outbox;

namespace ContentSeo.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddContentSeoInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");

        services.AddDbContext<ContentSeoDbContext>(options =>
            options.UseSqlServer(
                connectionString,
                sql =>
                {
                    sql.MigrationsHistoryTable("__EFMigrationsHistory", "content_seo");
                    sql.EnableRetryOnFailure(3);
                }));

        // ── Unit of work / persistence plumbing ──────────────────────────────
        services.AddScoped<IUnitOfWork<ContentSeoDbContext>, UnitOfWork<ContentSeoDbContext>>();
        services.AddScoped<IContentSeoUnitOfWork, ContentSeoUnitOfWork>();
        services.AddScoped<IContentSeoInboxStore, ContentSeoInboxStore>();
        services.AddScoped<IModuleDbInitializer, ContentSeoDbInitializer>();

        // ── Repositories (PW-5) ─────────────────────────────────────────────
        services.AddScoped<ISeoMetadataRepository, SeoMetadataRepository>();
        services.AddScoped<IRedirectRepository, RedirectRepository>();
        services.AddScoped<IFaqItemRepository, FaqItemRepository>();
        services.AddScoped<ISitemapEntryRepository, SitemapEntryRepository>();
        services.AddScoped<IWeatherCacheRepository, WeatherCacheRepository>();

        // ── Authorization (PW-6) ────────────────────────────────────────────
        services.AddSingleton<IPermissionCatalog, ContentSeoPermissionCatalog>();

        // ── MediatR (handlers + domain-event notification) ──────────────────
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));

        // ── Outbox processor + cleaner ──────────────────────────────────────
        services.AddScoped<IOutboxProcessor, OutboxProcessor<ContentSeoDbContext>>();
        services.AddScoped<IOutboxCleaner, OutboxCleaner<ContentSeoDbContext>>();

        // ── Task 4 — External services (stubs) + Sitemap renderer ──────────
        services.AddSingleton<IWeatherProvider, NoOpWeatherProvider>();
        services.AddSingleton<ISearchConsolePinger, NoOpSearchConsolePinger>();
        services.AddScoped<ISitemapRenderer, SitemapRenderer>();

        // ── Task 4 — Background services ───────────────────────────────────
        services.AddHostedService<SitemapRegenerationService>();
        services.AddHostedService<WeatherPreFetchService>();

        return services;
    }
}
