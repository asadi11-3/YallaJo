using ContentCore.Contracts.Attachments;
using ContentCore.Contracts.Authorization;
using ContentCore.Contracts.Storage;
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
        services.AddScoped<IPromoBlockRepository, PromoBlockRepository>();

        services.AddScoped<IAttachmentExistenceService, AttachmentExistenceService>();
        services.AddScoped<IPublicEntityImageReader, PublicEntityImageReader>();
        services.AddScoped<IEntityAttachmentCleanupService, EntityAttachmentCleanupService>();
        services.AddScoped<IEntityCategoryRepository, EntityCategoryRepository>();
        services.AddScoped<IEntityTagRepository, EntityTagRepository>();
        services.AddScoped<ICategoryHierarchyService, CategoryHierarchyService>();

        // ── Unit of Work & Infrastructure ────────────────────────────────────────
        services.AddScoped<IContentCoreUnitOfWork, ContentCoreUnitOfWork>();
        services.AddScoped<IUnitOfWork<ContentCoreDbContext>, UnitOfWork<ContentCoreDbContext>>();
        services.AddScoped<IModuleDbInitializer, ContentCoreDbInitializer>();
        // DEV-SEED-B1: Development/QA-only seeder (guarded internally by IHostEnvironment.IsDevelopment()).
        services.AddScoped<IModuleDbInitializer, DevImagesSeeder>();
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));
        services.AddScoped<IOutboxProcessor, OutboxProcessor<ContentCoreDbContext>>();
        services.AddScoped<IOutboxCleaner, OutboxCleaner<ContentCoreDbContext>>();
        services.AddScoped<IContentCoreOutboxWriter, ContentCoreOutboxWriter>();

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

        // ── Translation Backfill Store (CONTENTCORE-FOLLOWUP-BACKFILL-001) ──
        // Application port that encapsulates ContentCoreDbContext for the
        // TriggerTranslationBackfillCommandHandler so the Application layer
        // never depends on the Infrastructure DbContext directly.
        services.AddScoped<ITranslationBackfillStore, TranslationBackfillStore>();

        // ── Cross-module ownership resolver ──────────────────────────────────
        // Fans out by EntityType to per-module ownership probes registered by
        // each owning module's *.Infrastructure DI (IPlaceOwnershipService,
        // ITourOwnershipService, IBlogOwnershipService, IReviewOwnershipService,
        // ITourGuideOwnershipService). ContentCore.Application depends only on
        // those modules' Contracts projects.
        services.AddScoped<IEntityOwnershipResolver, EntityOwnershipResolver>();
        services.AddScoped<IOwnershipGuard, OwnershipGuard>();

        // ── File Storage ─────────────────────────────────────────────────────
        services.AddScoped<IFileStorageService, LocalFileStorageService>();

        // ── FileAsset Registrar (Patch 2B) ──────────────────────────────────
        // Cross-module port: callers (e.g. Accounts backfill) never touch
        // ContentCoreDbContext directly. Idempotent on UX_FileAssets_StorageKey.
        services.AddScoped<IFileAssetRegistrar, FileAssetRegistrar>();

        // ── FileAsset Locator (Patch 2C) ───────────────────────────────────
        // Cross-module read port for the FileAsset V2 download path. Callers
        // (e.g. Accounts provider-document download) get a server-internal
        // FileAssetView that includes StorageKey for feeding into
        // IFileStorageService.OpenReadByStorageKeyAsync — never client-exposed.
        services.AddScoped<IFileAssetLocator, FileAssetLocator>();

        // Cross-module pre-flight migration probe used by Patch 2B backfills
        // to verify the AddFileAssets migration is applied on content_core.
        services.AddScoped<IBackfillContentCoreMigrationsProbe, BackfillContentCoreMigrationsProbe>();

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
