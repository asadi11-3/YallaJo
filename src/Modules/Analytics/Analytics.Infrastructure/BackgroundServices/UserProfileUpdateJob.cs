using Analytics.Application.Interfaces;
using Analytics.Application.Interfaces.Repositories;
using Analytics.Domain.Entities;
using Analytics.Domain.Enums;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Analytics.Infrastructure.BackgroundServices;

/// <summary>
/// Nightly job that aggregates last-90-day UserInteractions into UserPreference vectors.
/// Computes BudgetTier from average interacted price and IsFamilyTraveler from booking patterns.
/// </summary>
public sealed class UserProfileUpdateJob(
    IServiceScopeFactory scopeFactory,
    ILogger<UserProfileUpdateJob> logger) : BackgroundService
{
    private static readonly TimeSpan StartupDelay = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan Interval = TimeSpan.FromHours(24);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(StartupDelay, stoppingToken).ConfigureAwait(false);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunProfileUpdateAsync(stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "UserProfileUpdateJob failed — will retry in {Interval}", Interval);
            }

            await Task.Delay(Interval, stoppingToken).ConfigureAwait(false);
        }
    }

    private async Task RunProfileUpdateAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var interactionRepo = scope.ServiceProvider.GetRequiredService<IUserInteractionRepository>();
        var preferenceRepo = scope.ServiceProvider.GetRequiredService<IUserPreferenceRepository>();
        var snapshotRepo = scope.ServiceProvider.GetRequiredService<IEntityAttributeSnapshotRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IAnalyticsUnitOfWork>();

        logger.LogInformation("UserProfileUpdateJob started");

        // Get distinct user IDs from recent interactions (last 90 days)
        var cutoff = DateTime.UtcNow.AddDays(-90);
        var now = DateTime.UtcNow;

        // Process users in pages — get interactions page by page
        long? afterId = null;
        var processedUsers = new HashSet<Guid>();

        while (true)
        {
            var (interactions, nextId) = await interactionRepo.GetPageAsync(
                userId: null, entityType: null, entityId: null, type: null,
                from: cutoff, to: null, afterId: afterId, pageSize: 500, ct).ConfigureAwait(false);

            if (interactions.Count == 0) break;

            foreach (var interaction in interactions)
            {
                if (interaction.UserId is null || !processedUsers.Add(interaction.UserId.Value))
                    continue;

                await UpdateUserProfileAsync(interaction.UserId.Value, interactionRepo, preferenceRepo, snapshotRepo, unitOfWork, cutoff, now, ct).ConfigureAwait(false);
            }

            if (nextId is null) break;
            afterId = nextId;
        }

        logger.LogInformation("UserProfileUpdateJob completed — processed {Count} user profiles", processedUsers.Count);
    }

    private async Task UpdateUserProfileAsync(
        Guid userId,
        IUserInteractionRepository interactionRepo,
        IUserPreferenceRepository preferenceRepo,
        IEntityAttributeSnapshotRepository snapshotRepo,
        IAnalyticsUnitOfWork unitOfWork,
        DateTime cutoff,
        DateTime now,
        CancellationToken ct)
    {
        try
        {
            // Get user's recent interactions
            var (interactions, _) = await interactionRepo.GetPageAsync(
                userId: userId, entityType: null, entityId: null, type: null,
                from: cutoff, to: null, afterId: null, pageSize: 1000, ct).ConfigureAwait(false);

            if (interactions.Count == 0) return;

            // Bulk-load all referenced snapshots to avoid N+1
            var entityKeys = interactions.Select(i => (i.EntityType, i.EntityId)).Distinct();
            var snapshotMap = await snapshotRepo.GetByEntitiesAsync(entityKeys, ct).ConfigureAwait(false);

            // Compute budget tier from average interacted entity price
            var prices = new List<decimal>();
            var hasChildBooking = false;

            foreach (var interaction in interactions)
            {
                snapshotMap.TryGetValue((interaction.EntityType, interaction.EntityId), out var snapshot);
                if (snapshot?.BasePriceAmount is > 0)
                    prices.Add(snapshot.BasePriceAmount.Value);

                // 4.5: Family inference — check if any booking involves child-friendly entity
                if (interaction.InteractionType is InteractionType.BookingCompleted or InteractionType.BookingStarted
                    && snapshot?.IsChildFriendly == true)
                {
                    hasChildBooking = true;
                }
            }

            var avgPrice = prices.Count > 0 ? prices.Average() : 0m;
            var budgetTier = avgPrice switch
            {
                < 30m => "Budget",
                < 100m => "Mid",
                _ => "Luxury"
            };

            // Upsert user preference
            var preference = await preferenceRepo.GetByUserIdAsync(userId, ct).ConfigureAwait(false);
            if (preference is null)
            {
                preference = UserPreference.Create(userId, budgetTier, hasChildBooking, null, now);
                await preferenceRepo.AddAsync(preference, ct).ConfigureAwait(false);
            }
            else
            {
                preference.Update(budgetTier, hasChildBooking || preference.IsFamilyTraveler, preference.CurrentTripStage, now);
            }

            // Compute preferred categories from interaction weights (using already-loaded snapshotMap)
            var categoryScores = new Dictionary<Guid, decimal>();
            foreach (var interaction in interactions)
            {
                if (!snapshotMap.TryGetValue((interaction.EntityType, interaction.EntityId), out var catSnapshot)
                    || catSnapshot.CategoryIdsJson is null) continue;

                var weight = GetInteractionWeight(interaction.InteractionType);
                var categoryIds = V1ContentSimilarityScorer_ParseCategoryIds(catSnapshot.CategoryIdsJson);

                foreach (var catId in categoryIds)
                {
                    categoryScores.TryGetValue(catId, out var current);
                    categoryScores[catId] = current + weight;
                }
            }

            // Upsert top categories
            foreach (var (categoryId, score) in categoryScores.OrderByDescending(kv => kv.Value).Take(20))
            {
                await preferenceRepo.UpsertPreferredCategoryAsync(
                    UserPreferredCategory.Create(userId, categoryId, Math.Round(score, 4)),
                    ct).ConfigureAwait(false);
            }

            await unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to update profile for user {UserId}", userId);
        }
    }

    /// <summary>
    /// Position-bias propensity table (6.4).
    /// Positions 1/2/3/5/10 have known propensities; linear interpolation between.
    /// </summary>
    private static readonly (int Position, decimal Propensity)[] PropensityTable =
    [
        (1, 1.00m), (2, 0.55m), (3, 0.40m), (5, 0.25m), (10, 0.13m)
    ];

    private static decimal GetPropensity(int position)
    {
        if (position <= 1) return 1.00m;
        if (position >= 10) return 0.13m;

        for (var i = 0; i < PropensityTable.Length - 1; i++)
        {
            var (p1, v1) = PropensityTable[i];
            var (p2, v2) = PropensityTable[i + 1];
            if (position >= p1 && position <= p2)
            {
                var t = (decimal)(position - p1) / (p2 - p1);
                return v1 + t * (v2 - v1);
            }
        }
        return 0.13m;
    }

    private static decimal GetInteractionWeight(InteractionType type) => type switch
    {
        InteractionType.View => 0.5m,
        InteractionType.Click => 1.0m, // Corrected by propensity when position known
        InteractionType.Search => 0.3m,
        InteractionType.AddToFavorite => 2.0m,
        InteractionType.RemoveFromFavorite => -1.0m,
        InteractionType.BookingStarted => 1.5m,
        InteractionType.BookingCompleted => 3.0m,
        InteractionType.BookingCancelled => -0.5m,
        InteractionType.Share => 1.5m,
        InteractionType.ReviewSubmitted => 2.0m,
        InteractionType.Bookmark => 2.5m,
        InteractionType.NotInterested => -2.0m,
        _ => 0m
    };

    /// <summary>
    /// Apply propensity correction to click weight: correctedWeight = rawWeight / propensity(position).
    /// Used when position data is available from SuggestionMetrics.
    /// </summary>
    internal static decimal GetPropensityCorrectedWeight(InteractionType type, int? position)
    {
        var rawWeight = GetInteractionWeight(type);
        if (type != InteractionType.Click || position is null or <= 0)
            return rawWeight;
        return rawWeight / GetPropensity(position.Value);
    }

    /// <summary>Lightweight category parser without depending on scorer directly.</summary>
    private static IReadOnlyList<Guid> V1ContentSimilarityScorer_ParseCategoryIds(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return [];
        try { return System.Text.Json.JsonSerializer.Deserialize<Guid[]>(json) ?? []; }
        catch { return []; }
    }
}
