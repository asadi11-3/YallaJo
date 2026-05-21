using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Social.Application.Interfaces;
using Social.Domain.Enums;
using Social.Domain.Repositories;

namespace Social.Infrastructure.BackgroundServices;

/// <summary>
/// Weekly background service (Saturday 03:00 UTC) that soft-deletes Favorite rows
/// whose referenced entity (Tour / Place / Business) has been removed.
/// <br/>
/// V1: uses <see cref="IFavoriteRepository.GetByEntityAsync"/> against the cross-module
/// snapshot tables populated by content-deletion inbox handlers.
/// When a snapshot's IsDeleted flag is true the corresponding favorites are soft-deleted.
/// </summary>
internal sealed class OrphanedFavoritesCleanupService(
    IServiceScopeFactory scopeFactory,
    IOptions<OrphanedFavoritesCleanupOptions> options,
    ILogger<OrphanedFavoritesCleanupService> logger,
    TimeProvider timeProvider)
    : BackgroundService
{
    private readonly OrphanedFavoritesCleanupOptions _options = options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            logger.LogInformation("OrphanedFavoritesCleanupService is disabled via config.");
            return;
        }

        logger.LogInformation("OrphanedFavoritesCleanupService started. Target: {Day} {Time} UTC",
            _options.TargetDayOfWeek, _options.TargetTimeUtc);

        var initialDelay = ComputeDelayUntilNext(
            timeProvider.GetUtcNow().UtcDateTime,
            _options.TargetDayOfWeek,
            _options.TargetTimeUtc);
        logger.LogDebug("OrphanedFavoritesCleanup next run in {Delay}", initialDelay);

        try
        {
            await Task.Delay(initialDelay, stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            return;
        }

        using var timer = new PeriodicTimer(TimeSpan.FromDays(7));
        do
        {
            try
            {
                await RunCleanupAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "OrphanedFavoritesCleanupService run failed. Will retry next week.");
            }
        } while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task RunCleanupAsync(CancellationToken ct)
    {
        logger.LogInformation("OrphanedFavoritesCleanupService run started at {Now}", timeProvider.GetUtcNow());

        await using var scope = scopeFactory.CreateAsyncScope();
        var favoriteRepo = scope.ServiceProvider.GetRequiredService<IFavoriteRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<ISocialUnitOfWork>();

        var totalRemoved = 0;

        // For each entity type, check snapshot table for deleted entries and clean orphans.
        // V1 stub: snapshot tables are populated by content-deletion inbox handlers (T3 phase-2).
        // This loop structure is in place; concrete snapshot repo queries will be added when
        // content-tours.tour.deleted.v1 / content-places.*.deleted.v1 events are registered.
        foreach (FavoriteEntityType entityType in Enum.GetValues<FavoriteEntityType>())
        {
            var deletedEntityIds = await GetDeletedEntityIdsAsync(scope.ServiceProvider, entityType, ct);
            if (deletedEntityIds.Count == 0)
                continue;

            foreach (var entityId in deletedEntityIds.Take(_options.BatchSize))
            {
                var favorites = await favoriteRepo.GetByEntityAsync(entityType, entityId, ct);
                foreach (var fav in favorites.Where(f => !f.IsDeleted))
                {
                    fav.SoftDelete();
                    totalRemoved++;
                }
            }
        }

        if (totalRemoved > 0)
        {
            await unitOfWork.SaveChangesAsync(ct);
            logger.LogInformation("OrphanedFavoritesCleanup removed {Count} orphaned favorites", totalRemoved);
        }
        else
        {
            logger.LogInformation("OrphanedFavoritesCleanup: no orphaned favorites found");
        }
    }

    /// <summary>
    /// Returns entity IDs whose snapshot has IsDeleted=true.
    /// V1: returns empty (snapshot repo injection is Phase-2 when content-deletion events land).
    /// </summary>
    private async Task<IReadOnlyList<Guid>> GetDeletedEntityIdsAsync(
        IServiceProvider serviceProvider, FavoriteEntityType entityType, CancellationToken ct)
        => entityType switch
        {
            FavoriteEntityType.Tour => await serviceProvider
                .GetRequiredService<ITourSnapshotRepository>()
                .GetDeletedTourIdsAsync(_options.BatchSize, ct),
            FavoriteEntityType.Place => await serviceProvider
                .GetRequiredService<IPlaceSnapshotRepository>()
                .GetDeletedPlaceIdsAsync(_options.BatchSize, ct),
            FavoriteEntityType.Business => await serviceProvider
                .GetRequiredService<IBusinessSnapshotRepository>()
                .GetDeletedBusinessIdsAsync(_options.BatchSize, ct),
            _ => []
        };

    /// <summary>Computes the delay to the next occurrence of <paramref name="targetDay"/> at <paramref name="targetTime"/> UTC.</summary>
    internal static TimeSpan ComputeDelayUntilNext(DateTime now, DayOfWeek targetDay, TimeSpan targetTime)
    {
        var targetUtc = now.Date.Add(targetTime);
        var daysUntilTarget = ((int)targetDay - (int)now.DayOfWeek + 7) % 7;

        if (daysUntilTarget == 0 && now.TimeOfDay >= targetTime)
            daysUntilTarget = 7; // already past this week's window

        targetUtc = targetUtc.AddDays(daysUntilTarget);
        return targetUtc - now;
    }
}
