using ContentCore.Contracts.Authorization;
using ContentCore.Application.Authorization;
using ContentCore.Application.Interfaces;
using YallaJo.SharedKernel.Application.Authorization;
using ContentCore.Domain.Repositories;
using ContentCore.Domain.Services;
using ContentCore.Infrastructure.BackgroundJobs;
using ContentCore.Infrastructure.Persistence;
using ContentCore.Infrastructure.Persistence.Seeding;
using ContentCore.Infrastructure.Repositories;
using ContentCore.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Http.Resilience;
using YallaJo.SharedKernel.Application.Abstractions.Storage;
using YallaJo.SharedKernel.Application.Abstractions.Translation;
using YallaJo.SharedKernel.Infrastructure.Outbox;
using YallaJo.SharedKernel.Infrastructure.BackgroundJobs;
using YallaJo.SharedKernel.Infrastructure.Data;

namespace ContentCore.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddContentCoreInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");

        services.AddDbContext<ContentCoreDbContext>(options =>
            options.UseSqlServer(
                connectionString,
                sql =>
                {
                    sql.MigrationsHistoryTable("__EFMigrationsHistory", "content_core");
                    sql.EnableRetryOnFailure(3);
                }));

        // ── Repositories ────────────────────────────────────────────────────────
        services.AddScoped<ICategoryRepository, CategoryRepository>();
        services.AddScoped<ILanguageRepository, LanguageRepository>();
        services.AddScoped<ITagRepository, TagRepository>();
        services.AddScoped<ISpecializationRepository, SpecializationRepository>();
        services.AddScoped<ITranslationCacheRepository, TranslationCacheRepository>();
        services.AddScoped<IAttachmentRepository, AttachmentRepository>();
        services.AddScoped<IEntityCategoryRepository, EntityCategoryRepository>();
        services.AddScoped<IEntityTagRepository, EntityTagRepository>();
        services.AddScoped<ICategoryHierarchyService, CategoryHierarchyService>();

        // ── Unit of Work & Infrastructure ────────────────────────────────────────
        services.AddScoped<IContentCoreUnitOfWork, ContentCoreUnitOfWork>();
        services.AddScoped<IUnitOfWork<ContentCoreDbContext>, UnitOfWork<ContentCoreDbContext>>();
        services.AddScoped<IModuleDbInitializer, ContentCoreDbInitializer>();
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));
        services.AddScoped<IOutboxProcessor, OutboxProcessor<ContentCoreDbContext>>();
        services.AddScoped<IOutboxCleaner, OutboxCleaner<ContentCoreDbContext>>();

        // ── Translation Service (decorator pattern) ─────────────────────────────
        // 1. Register the concrete Azure provider as a named/keyed inner service
        // HTTP resilience: retry 3x (exponential backoff+jitter), circuit breaker, 10s per-attempt timeout.
        // DisableForUnsafeHttpMethods=true: translation POSTs are NOT retried (non-idempotent).
        services.AddHttpClient<AzureTranslateService>()
            .AddStandardResilienceHandler(options =>
            {
                options.Retry.MaxRetryAttempts = 3;
                options.CircuitBreaker.FailureRatio = 0.5;
                options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(10);
            });

        // 2. Register ITranslationService as the AutoSaveTranslationService decorator
        //    wrapping the AzureTranslateService inner implementation.
        //    AutoSaveTranslationService persists translation cache rows immediately via
        //    ITranslationCacheRepository.TryAddCacheEntryAsync (raw INSERT ... WHERE NOT EXISTS
        //    with UPDLOCK/HOLDLOCK for race-safe dedup). Cache writes are auxiliary and do
        //    NOT participate in the caller's UnitOfWork — callers do not need to call
        //    SaveChangesAsync to persist cache rows.
        services.AddScoped<ITranslationService>(sp =>
            new AutoSaveTranslationService(
                inner: sp.GetRequiredService<AzureTranslateService>(),
                cacheRepository: sp.GetRequiredService<ITranslationCacheRepository>(),
                logger: sp.GetRequiredService<ILogger<AutoSaveTranslationService>>()));

        // ── Entity Translation Orchestrator ──────────────────────────────────
        services.AddScoped<IActiveLanguageProvider, ActiveLanguageProvider>();
        services.AddScoped<IEntityTranslationOrchestrator, EntityTranslationOrchestrator>();

        // ── Cross-module ownership resolver ──────────────────────────────────
        // Fans out by EntityType to per-module ownership probes registered by
        // each owning module's *.Infrastructure DI (IPlaceOwnershipService,
        // ITourOwnershipService, IBlogOwnershipService, IReviewOwnershipService,
        // ITourGuideOwnershipService). ContentCore.Application depends only on
        // those modules' Contracts projects.
        services.AddScoped<IEntityOwnershipResolver, EntityOwnershipResolver>();

        // ── File Storage ─────────────────────────────────────────────────────
        services.AddScoped<IFileStorageService, LocalFileStorageService>();

        // ── Media Processing (background jobs) ─────────────────────────────
        services.AddSingleton<MediaProcessingQueue>();
        services.AddSingleton<IMediaProcessingQueue>(sp => sp.GetRequiredService<MediaProcessingQueue>());
        services.AddScoped<IImageProcessingService, ImageProcessingService>();
        services.AddScoped<IVideoProcessingService, VideoProcessingService>();
        // ffmpeg v8.0.1 confirmed installed (BinaryFolder configured in appsettings.json "FFmpeg:BinaryFolder").
        // SixLabors.ImageSharp handles image processing (no external binary needed).
        services.AddHostedService<MediaProcessingBackgroundService>();

        // ── Permission catalog (discovered by Security.Infrastructure PermissionSeeder) ─
        services.AddSingleton<IPermissionCatalog, ContentCorePermissionCatalog>();

        return services;
    }
}
