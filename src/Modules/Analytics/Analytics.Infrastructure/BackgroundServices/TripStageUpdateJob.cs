using Analytics.Application.Interfaces.Repositories;
using Analytics.Domain.Enums;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Analytics.Infrastructure.BackgroundServices;

/// <summary>
/// Daily background job that recomputes trip stage for users based on tracking sessions.
/// For V2.5 — listens to LiveTrackingSessionStarted/Ended events (Phase 2 B5)
/// and falls back to daily inference from recent interaction patterns.
/// </summary>
internal sealed class TripStageUpdateJob(
    IServiceScopeFactory scopeFactory,
    ILogger<TripStageUpdateJob> logger) : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromHours(24);
    private static readonly TimeSpan StartupDelay = TimeSpan.FromMinutes(10);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(StartupDelay, stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await UpdateTripStagesAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "TripStageUpdateJob failed");
            }

            await Task.Delay(Interval, stoppingToken);
        }
    }

    private async Task UpdateTripStagesAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var preferenceRepo = scope.ServiceProvider.GetRequiredService<IUserPreferenceRepository>();
        var interactionRepo = scope.ServiceProvider.GetRequiredService<IUserInteractionRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<Analytics.Application.Interfaces.IAnalyticsUnitOfWork>();

        logger.LogInformation("TripStageUpdateJob: computing trip stages from interaction patterns");

        var now = DateTime.UtcNow;
        var cutoff = now.AddDays(-30);
        long? afterId = null;
        var userInteractions = new Dictionary<Guid, List<(InteractionType Type, DateTime At)>>();

        // Collect recent interactions per user
        while (true)
        {
            var (interactions, nextId) = await interactionRepo.GetPageAsync(
                userId: null, entityType: null, entityId: null, type: null,
                from: cutoff, to: null, afterId: afterId, pageSize: 500, ct).ConfigureAwait(false);

            foreach (var i in interactions)
            {
                if (i.UserId is not { } uid) continue;
                if (!userInteractions.TryGetValue(uid, out var list))
                {
                    list = [];
                    userInteractions[uid] = list;
                }
                list.Add((i.InteractionType, i.OccurredAt));
            }

            if (interactions.Count < 500 || nextId is null) break;
            afterId = nextId;
        }

        var updated = 0;
        foreach (var (userId, interactions) in userInteractions)
        {
            var stage = InferTripStage(interactions, now);
            var preference = await preferenceRepo.GetByUserIdAsync(userId, ct).ConfigureAwait(false);
            if (preference is null) continue;

            var stageStr = stage.ToString();
            if (preference.CurrentTripStage == stageStr) continue;

            preference.Update(preference.BudgetTier, preference.IsFamilyTraveler, stageStr, now);
            updated++;
        }

        if (updated > 0)
            await unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);

        logger.LogInformation("TripStageUpdateJob: updated {Count} user trip stages", updated);
    }

    /// <summary>
    /// Infer trip stage from recent interaction patterns.
    /// Will be refined when LiveTrackingSession events are consumed.
    /// </summary>
    private static TripStage InferTripStage(List<(InteractionType Type, DateTime At)> interactions, DateTime now)
    {
        if (interactions.Count == 0)
            return TripStage.None;

        var lastInteraction = interactions.Max(i => i.At);
        var daysSinceLastInteraction = (now - lastInteraction).TotalDays;

        var hasRecentBooking = interactions.Any(i =>
            i.Type == InteractionType.BookingCompleted && (now - i.At).TotalDays <= 7);
        var hasVeryRecentBooking = interactions.Any(i =>
            i.Type == InteractionType.BookingCompleted && (now - i.At).TotalDays <= 1);
        var hasTodayViews = interactions.Any(i =>
            i.Type == InteractionType.View && (now - i.At).TotalHours <= 24);

        // Just arrived (booking completed in last 24h)
        if (hasVeryRecentBooking && hasTodayViews)
            return TripStage.JustLanded;

        // Mid-trip (booked recently + actively viewing)
        if (hasRecentBooking && hasTodayViews)
            return TripStage.MidTrip;

        // Last day (recent booking but gone quiet for 3+ days)
        if (hasRecentBooking && daysSinceLastInteraction >= 3)
            return TripStage.LastDay;

        // No interactions for 30+ days → post-trip
        if (daysSinceLastInteraction >= 30)
            return TripStage.PostTrip;

        // Active browsing with no booking → pre-trip
        var viewCount = interactions.Count(i => i.Type is InteractionType.View or InteractionType.Search);
        var bookingCount = interactions.Count(i => i.Type is InteractionType.BookingCompleted or InteractionType.BookingStarted);
        if (viewCount >= 3 && bookingCount == 0)
            return TripStage.Pre;

        return TripStage.None;
    }
}
