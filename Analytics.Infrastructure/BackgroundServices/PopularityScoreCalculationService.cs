using Analytics.Application.Interfaces;
using Analytics.Application.Interfaces.Repositories;
using Analytics.Contracts.IntegrationEvents;
using Analytics.Domain.Enums;
using Analytics.Domain.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Analytics.Infrastructure.BackgroundServices;

internal sealed class PopularityScoreCalculationService(
    IServiceScopeFactory scopeFactory,
    IOptions<PopularityScoreCalculationOptions> options,
    ILogger<PopularityScoreCalculationService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var settings = options.Value;
        if (!settings.Enabled)
        {
            logger.LogInformation("Analytics popularity score calculation is disabled");
            return;
        }

        await Task.Delay(settings.InitialDelay, stoppingToken);
        using var timer = new PeriodicTimer(settings.Interval);
        do
        {
            await ProcessBatchAsync(settings, stoppingToken);
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task ProcessBatchAsync(PopularityScoreCalculationOptions settings, CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var scores = scope.ServiceProvider.GetRequiredService<IPopularityScoreRepository>();
        var interactions = scope.ServiceProvider.GetRequiredService<IUserInteractionRepository>();
        var snapshots = scope.ServiceProvider.GetRequiredService<IEntityPopularitySnapshotRepository>();
        var outbox = scope.ServiceProvider.GetRequiredService<IAnalyticsOutboxWriter>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IAnalyticsUnitOfWork>();

        var now = DateTime.UtcNow;
        var stale = await scores.GetStaleAsync(settings.BatchSize, ct);
        foreach (var score in stale)
        {
            var aggregate = await interactions.AggregateScoreAsync(score.EntityType, score.EntityId, now, ct);
            var finalScore = aggregate.ViewScore + aggregate.ClickScore + aggregate.FavoriteScore + aggregate.BookingStartedScore + aggregate.BookingCompletedScore + aggregate.ReviewScore;
            score.Recalculate(finalScore, aggregate.InteractionCount, now);
        }

        await snapshots.SnapshotCurrentAsync(now, ct);

        var trendingCount = 0;
        var entityTypes = new[] { EntityType.Tour, EntityType.Place, EntityType.Business, EntityType.Category };
        foreach (var type in entityTypes)
        {
            var byType = await scores.GetAllByTypeAsync(type, ct);
            var ranked = new List<(Analytics.Domain.Entities.PopularityScore Score, decimal Delta)>();
            foreach (var score in byType)
            {
                var history = await snapshots.GetRecentByEntityAsync(type, score.EntityId, 8, ct);
                var sevenDay = history.Where(x => x.TakenAt <= now.AddDays(-7)).OrderByDescending(x => x.TakenAt).FirstOrDefault();
                var delta = sevenDay is null ? score.Score * 0.5m : score.Score - sevenDay.Score;
                ranked.Add((score, delta));
            }

            var top = ranked.OrderByDescending(x => x.Delta).Take(50).Select((x, index) => new { x.Score, Rank = index + 1 }).ToDictionary(x => x.Score.Id, x => x.Rank);
            trendingCount += top.Count;
            foreach (var score in byType)
            {
                score.SetTrendingRank(top.TryGetValue(score.Id, out var rank) ? rank : null, now);
            }
        }

        if (stale.Count > 0)
        {
            await outbox.WriteAsync(new PopularityScoresRecalculatedIntegrationEvent(entityTypes.Select(x => x.ToString()).ToList(), stale.Count, now), ct);
        }

        await outbox.WriteAsync(new TrendingRefreshedIntegrationEvent(entityTypes.Select(x => x.ToString()).ToList(), trendingCount, now), ct);
        await unitOfWork.SaveChangesAsync(ct);
        logger.LogInformation("Recalculated {Count} popularity scores and refreshed {TrendingCount} trending ranks", stale.Count, trendingCount);
    }
}
