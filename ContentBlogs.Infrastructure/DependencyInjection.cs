using ContentBlogs.Application.Interfaces;
using ContentBlogs.Contracts.Authorization;
using ContentBlogs.Infrastructure.Persistence;
using ContentBlogs.Infrastructure.Persistence.Seeding;
using ContentBlogs.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using YallaJo.SharedKernel.Infrastructure.Data;
using YallaJo.SharedKernel.Infrastructure.Outbox;
using YallaJo.SharedKernel.Infrastructure.BackgroundJobs;

namespace ContentBlogs.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddContentBlogsInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");

        services.AddDbContext<ContentBlogsDbContext>(options =>
            options.UseSqlServer(
                connectionString,
                sql =>
                {
                    sql.MigrationsHistoryTable("__EFMigrationsHistory", "content_blogs");
                    sql.EnableRetryOnFailure(3);
                }));

        services.AddScoped<IUnitOfWork<ContentBlogsDbContext>, UnitOfWork<ContentBlogsDbContext>>();
        services.AddScoped<IContentBlogsUnitOfWork, ContentBlogsUnitOfWork>();
        services.AddScoped<IContentBlogsInboxStore, ContentBlogsInboxStore>();
        services.AddScoped<IModuleDbInitializer, ContentBlogsDbInitializer>();
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));
        services.AddScoped<IOutboxProcessor, OutboxProcessor<ContentBlogsDbContext>>();
        services.AddScoped<IOutboxCleaner, OutboxCleaner<ContentBlogsDbContext>>();

        // ── Cross-module read-only services ──────────────────────────────────
        // Owned & implemented here so consumers (ContentCore, etc.) depend only on
        // ContentBlogs.Contracts and never on the Blogs schema directly.
        services.AddScoped<IBlogOwnershipService, BlogOwnershipService>();

        return services;
    }
}
