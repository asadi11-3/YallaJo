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
using YallaJo.SharedKernel.Infrastructure.Inbox;
using YallaJo.SharedKernel.Infrastructure.Outbox;
using Microsoft.Extensions.Hosting;
using Social.Infrastructure.BackgroundServices;
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
        // FE-2D development-only accessibility-review smoke data (Development-gated by UseDataSeedingAsync).
        services.AddScoped<IModuleDbInitializer, AccessibilityReviewSeeder>();
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));
        services.AddScoped<IOutboxProcessor, OutboxProcessor<SocialDbContext>>();
        services.AddScoped<IOutboxCleaner, OutboxCleaner<SocialDbContext>>();

        // ── Cross-module read-only services ──────────────────────────────────
        services.AddScoped<IReviewOwnershipService, ReviewOwnershipService>();

        // ── Unit of Work ──────────────────────────────────────────────────────
        services.AddScoped<ISocialUnitOfWork, SocialUnitOfWork>();

        // ── Inbox store ───────────────────────────────────────────────────────
        services.AddScoped<ISocialInboxStore, SocialInboxStore>();

        // ── Repositories ──────────────────────────────────────────────────────
        services.AddScoped<IReviewRepository, ReviewRepository>();
        services.AddScoped<IAccessibilityReviewRepository, AccessibilityReviewRepository>();
        services.AddScoped<IFavoriteRepository, FavoriteRepository>();
        services.AddScoped<IReportRepository, ReportRepository>();
        services.AddScoped<IContentModerationLogRepository, ContentModerationLogRepository>();
        services.AddScoped<IBookingEligibilitySnapshotRepository, BookingEligibilitySnapshotRepository>();
        services.AddScoped<IPlaceSnapshotRepository, PlaceSnapshotRepository>();
        services.AddScoped<IBusinessSnapshotRepository, BusinessSnapshotRepository>();
        services.AddScoped<ITourSnapshotRepository, TourSnapshotRepository>();
        services.AddScoped<IEntityRatingCacheRepository, EntityRatingCacheRepository>();
        services.AddScoped<ISocialOutboxWriter, SocialOutboxWriter>();
        services.AddScoped<IReviewHelpfulVoteRepository, ReviewHelpfulVoteRepository>();
        services.AddScoped<IUserModerationRepository, UserModerationRepository>();

        // ── Content moderation ────────────────────────────────────────────────
        // BlocklistProfanityFilter is Singleton: loads word list from DB on first use
        services.AddSingleton<IProfanityFilter, BlocklistProfanityFilter>();
        // AlwaysSafeNsfwClassifier: stub for dev/test; replace with real classifier in prod
        services.AddSingleton<INsfwClassifier, AlwaysSafeNsfwClassifier>();

        // ── Permission catalog ────────────────────────────────────────────────
        services.AddSingleton<IPermissionCatalog, SocialPermissionCatalog>();

        // ── Background services ───────────────────────────────────────────────
        services.Configure<OrphanedFavoritesCleanupOptions>(opts =>
        {
            var section = configuration.GetSection(OrphanedFavoritesCleanupOptions.SectionName);
            if (bool.TryParse(section[nameof(OrphanedFavoritesCleanupOptions.Enabled)], out var enabled))
                opts.Enabled = enabled;
            if (Enum.TryParse<DayOfWeek>(section[nameof(OrphanedFavoritesCleanupOptions.TargetDayOfWeek)], out var day))
                opts.TargetDayOfWeek = day;
            if (TimeSpan.TryParse(section[nameof(OrphanedFavoritesCleanupOptions.TargetTimeUtc)], out var time))
                opts.TargetTimeUtc = time;
            if (int.TryParse(section[nameof(OrphanedFavoritesCleanupOptions.BatchSize)], out var batch))
                opts.BatchSize = batch;
        });
        services.AddHostedService<OrphanedFavoritesCleanupService>();

        services.Configure<RatingRecalculationOptions>(opts =>
        {
            var section = configuration.GetSection(RatingRecalculationOptions.SectionName);
            if (bool.TryParse(section[nameof(RatingRecalculationOptions.Enabled)], out var enabled))
                opts.Enabled = enabled;
            if (TimeSpan.TryParse(section[nameof(RatingRecalculationOptions.TargetTimeUtc)], out var time))
                opts.TargetTimeUtc = time;
            if (decimal.TryParse(section[nameof(RatingRecalculationOptions.GlobalAverageRating)],
                System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture, out var avg))
                opts.GlobalAverageRating = avg;
            if (decimal.TryParse(section[nameof(RatingRecalculationOptions.ConfidenceConstant)],
                System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture, out var c))
                opts.ConfidenceConstant = c;
            if (int.TryParse(section[nameof(RatingRecalculationOptions.MinReviewsToShow)], out var min))
                opts.MinReviewsToShow = min;
        });
        services.AddHostedService<RatingRecalculationService>();

        return services;
    }
}
