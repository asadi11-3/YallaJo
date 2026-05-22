using ContentBlogs.Application.Authorization;
using ContentBlogs.Application.Interfaces;
using ContentBlogs.Contracts.Authorization;
using ContentBlogs.Domain.Repositories;
using ContentBlogs.Infrastructure.Configuration;
using ContentBlogs.Infrastructure.Persistence;
using ContentBlogs.Infrastructure.Persistence.Seeding;
using ContentBlogs.Infrastructure.Repositories;
using ContentBlogs.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using YallaJo.SharedKernel.Application.Authorization;
using YallaJo.SharedKernel.Infrastructure.BackgroundJobs;
using YallaJo.SharedKernel.Infrastructure.Data;
using ContentBlogs.Infrastructure.BackgroundServices;
using YallaJo.SharedKernel.Infrastructure.Outbox;

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

        services.AddScoped<IBlogRepository, BlogRepository>();
        services.AddScoped<IBlogCommentRepository,BlogCommentRepository>();
        services.AddScoped<IBlogTourRepository, BlogTourRepository>();

        // ── Creator Identity repositories ────────────────────────────────
        services.AddScoped<ICreatorApplicationRepository, CreatorApplicationRepository>();
        services.AddScoped<ICreatorProfileRepository, CreatorProfileRepository>();
        services.AddScoped<ICreatorInvitationRepository, CreatorInvitationRepository>();
        services.AddScoped<ICreatorFollowRepository, CreatorFollowRepository>();
        services.AddScoped<ICreatorNicheRepository, CreatorNicheRepository>();
        services.AddScoped<ICreatorPostRepository, CreatorPostRepository>();

        services.AddSingleton<IPermissionCatalog, ContentBlogsPermissionCatalog>();

        // ── Author-hierarchy authorization (Phase 1 closure) ─────────────────
        // BlogAuthorHierarchyGuard lives in ContentBlogs.Application and depends
        // on Security.Contracts.Authorization.IUserPrivilegeLevelReader.  The
        // implementation is registered by AddSecurityApplication(), so this
        // module no longer needs a ProjectReference to Security.Application nor
        // a local adapter — the IRoleHierarchyService coupling is gone.
        services.AddScoped<IBlogAuthorHierarchyGuard, BlogAuthorHierarchyGuard>();
        services.AddScoped<IBlogCommentAuthorizationGuard, BlogCommentAuthorizationGuard>();

        // ── Cross-module read-only services ──────────────────────────────────
        // Owned & implemented here so consumers (ContentCore, etc.) depend only on
        // ContentBlogs.Contracts and never on the Blogs schema directly.
        services.AddScoped<IBlogOwnershipService, BlogOwnershipService>();

        // Disclosure validation — stub until Accounts module provides a real implementation
        services.AddScoped<IProviderEntitiesReadClient, NullProviderEntitiesReadClient>();

        services.Configure<ContentBlogsViewerHashOptions>(
            configuration.GetSection(ContentBlogsViewerHashOptions.SectionName));
        services.AddSingleton<IBlogViewerHashService, BlogViewerHashService>();
        services.AddScoped<IBlogViewCounter, BlogViewCounter>();

        // ── Background services ──────────────────────────────────────────────
        services.AddHostedService<CreatorInvitationCleanupService>();
        services.AddHostedService<CreatorTierPromotionService>();
        services.AddHostedService<CreatorStatsRollupService>();

        return services;
    }
}
