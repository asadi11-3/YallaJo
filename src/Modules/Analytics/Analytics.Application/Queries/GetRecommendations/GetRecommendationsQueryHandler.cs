using Analytics.Application.Interfaces.Repositories;
using Analytics.Application.Models;
using Analytics.Domain.Enums;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Analytics.Application.Queries.GetRecommendations;

public sealed class GetRecommendationsQueryHandler(
    IRecommendationCacheRepository cacheRepository,
    IEntityAttributeSnapshotRepository snapshotRepository,
    IUserPreferenceRepository preferenceRepository,
    IUserExcludedEntityRepository excludedRepository,
    IEditorialPinRepository pinRepository,
    ILogger<GetRecommendationsQueryHandler> logger) : IQueryHandler<GetRecommendationsQuery, RecommendationsResponse>
{
    public async Task<Result<RecommendationsResponse>> Handle(GetRecommendationsQuery request, CancellationToken ct)
    {
        var limit = Math.Clamp(request.Limit, 1, 100);

        // Load user exclusions for filtering
        var exclusions = await excludedRepository.GetActiveByUserAsync(request.UserId, ct).ConfigureAwait(false);
        var excludedSet = exclusions.Select(e => (e.EntityKind, e.EntityId)).ToHashSet();

        // Load user preferences for budget/family filtering
        var preference = await preferenceRepository.GetByUserIdAsync(request.UserId, ct).ConfigureAwait(false);

        var cacheRows = await cacheRepository.GetByUserAsync(request.UserId, limit + excludedSet.Count, ct).ConfigureAwait(false);

        if (cacheRows.Count > 0)
        {
            // Bulk-load all referenced snapshots to avoid N+1
            var entityKeys = cacheRows.Select(r => (r.EntityKind, r.EntityId)).Distinct();
            var snapshotMap = await snapshotRepository.GetByEntitiesAsync(entityKeys, ct).ConfigureAwait(false);

            var items = new List<RecommendationItemDto>(cacheRows.Count);
            foreach (var row in cacheRows.OrderBy(row => row.Position))
            {
                if (items.Count >= limit) break;

                // 4.2: Skip excluded entities
                if (excludedSet.Contains((row.EntityKind, row.EntityId)))
                    continue;

                if (!snapshotMap.TryGetValue((row.EntityKind, row.EntityId), out var snapshot))
                    continue;

                // 4.4: Budget tier filter (unless showAllPrices)
                if (!request.ShowAllPrices && !IsWithinBudgetTier(snapshot.BasePriceAmount, preference?.BudgetTier))
                    continue;

                items.Add(RecommendationMapping.ToDto(snapshot, row.Score, row.SignalsJson));
            }

            // 3.7: Inject editorial pins into personalized feed
            var pins = await pinRepository.GetActiveByContextAsync(SuggestionContext.PersonalizedFeed, ct).ConfigureAwait(false);
            items = await EditorialPinInjector.InjectPins(items, pins, snapshotRepository, limit, ct).ConfigureAwait(false);

            logger.LogDebug("Read {Count} personalized recommendations for user {UserId}", items.Count, request.UserId);
            return Result.Success(new RecommendationsResponse(items, IsPersonalized: true, cacheRows.Max(row => row.GeneratedAt)));
        }

        var preferredCategories = await preferenceRepository.GetPreferredCategoriesAsync(request.UserId, ct).ConfigureAwait(false);
        var categoryIds = preferredCategories.Where(category => category.PreferenceScore > 0m).Select(category => category.CategoryId).ToHashSet();
        var tours = await snapshotRepository.GetActiveByKindAsync(EntityType.Tour, ct).ConfigureAwait(false);
        var fallbackItems = tours
            .Where(tour => !excludedSet.Contains((tour.EntityKind, tour.EntityId)))
            .Where(tour => RecommendationMapping.HasAnyCategory(tour, categoryIds))
            .Where(tour => request.ShowAllPrices || IsWithinBudgetTier(tour.BasePriceAmount, preference?.BudgetTier))
            .OrderByDescending(tour => tour.BookingCount)
            .ThenByDescending(tour => tour.AverageRating)
            .Take(limit)
            .Select(RecommendationMapping.ToPopularityDto)
            .ToList();

        // 4.7: Zero-result rescue — if no results after all filters, broaden to general popular items
        if (fallbackItems.Count == 0)
        {
            var rescueItems = tours
                .OrderByDescending(tour => tour.BookingCount)
                .ThenByDescending(tour => tour.AverageRating)
                .Take(limit)
                .Select(RecommendationMapping.ToPopularityDto)
                .ToList();

            logger.LogDebug("Zero-result rescue: returning {Count} popular items for user {UserId}", rescueItems.Count, request.UserId);
            return Result.Success(new RecommendationsResponse(rescueItems, IsPersonalized: false, ComputedAt: null));
        }

        logger.LogDebug("Read {Count} fallback recommendations for user {UserId}", fallbackItems.Count, request.UserId);
        return Result.Success(new RecommendationsResponse(fallbackItems, IsPersonalized: false, ComputedAt: null));
    }

    /// <summary>
    /// 4.4: Budget tier filtering. Returns true if the price is within ±1 tier of the user's budget.
    /// Budget tiers: Budget (<30), Mid (30-99), Luxury (≥100). Unknown = no filter.
    /// </summary>
    private static bool IsWithinBudgetTier(decimal? price, string? budgetTier)
    {
        if (string.IsNullOrWhiteSpace(budgetTier) || budgetTier == "Unknown" || price is null)
            return true;

        return budgetTier switch
        {
            "Budget" => price < 100m,   // Budget + Mid (±1 tier)
            "Mid" => true,              // Mid can see all (±1 includes Budget and Luxury)
            "Luxury" => price >= 30m,   // Luxury + Mid (±1 tier)
            _ => true
        };
    }
}
