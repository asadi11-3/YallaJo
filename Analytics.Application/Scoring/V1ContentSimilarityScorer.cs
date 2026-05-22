using System.Text.Json;
using Analytics.Domain.Entities;
using Analytics.Domain.Enums;
using YallaJo.SharedKernel.Domain.ValueObjects;

namespace Analytics.Application.Scoring;

public sealed class V1ContentSimilarityScorer : IRecommendationScoringEngine
{
    private const int CandidatePoolSize = 60;
    private const double MaxDistanceKm = 100d;
    private const int MaxPerPlaceId = 2;

    private static readonly WeightSet SimilarToursWeights = new(0.30m, 0.20m, 0.15m, 0.20m, 0.10m, 0.05m);

    private static readonly IReadOnlyDictionary<SuggestionContext, WeightSet> Weights = new Dictionary<SuggestionContext, WeightSet>
    {
        [SuggestionContext.SimilarTours] = SimilarToursWeights,
        [SuggestionContext.AddAMeal] = new(0.00m, 0.10m, 0.40m, 0.25m, 0.20m, 0.05m),
        [SuggestionContext.WhereToStay] = new(0.00m, 0.20m, 0.45m, 0.20m, 0.10m, 0.05m),
        [SuggestionContext.SimilarBusinesses] = new(0.30m, 0.15m, 0.30m, 0.15m, 0.05m, 0.05m),
        [SuggestionContext.ExploreNearby] = new(0.00m, 0.00m, 0.40m, 0.25m, 0.30m, 0.05m),
    };

    public IReadOnlyList<ScoredCandidate> Score(ScoringContext context)
    {
        var weights = Weights.GetValueOrDefault(context.SuggestionContext, SimilarToursWeights);
        var effectiveHalalOnly = context.HalalOnly || IsArabicLocale(context.AcceptLanguage);
        var now = DateTime.UtcNow;

        // Phase 1: Filter + score candidates
        var scored = context.Candidates
            .Where(c => c.EntityId != context.Source.EntityId || c.EntityKind != context.Source.EntityKind)
            .Where(c => !IsNegativeReviewed(c))                          // 3.3: negative review suppression
            .Where(c => !effectiveHalalOnly || c.IsHalal == true)        // 3.5: halal filter
            .Select(c => ScoreCandidate(context.Source, c, weights, now, context.ActiveBoosts, context.IsFamilyTraveler, context.SeasonalityMultipliers, context.ActiveHolidayRules, context.UserShareCount))
            .Where(c => c is not null)
            .Select(c => c!)
            .OrderByDescending(c => c.Score)
            .ThenByDescending(c => c.Snapshot.AverageRating)
            .ThenByDescending(c => c.Snapshot.BookingCount)
            .Take(CandidatePoolSize)
            .ToList();

        // Phase 2: MMR diversity selection (3.1)
        var lambda = SuggestionContextWeights.GetLambda(context.SuggestionContext);
        return ApplyMmrDiversity(scored, Math.Max(1, context.MaxResults), lambda);
    }

    private static ScoredCandidate? ScoreCandidate(
        EntityAttributeSnapshot source,
        EntityAttributeSnapshot candidate,
        WeightSet weights,
        DateTime now,
        IReadOnlyDictionary<(EntityType Kind, Guid Id), BoostPackage>? activeBoosts,
        bool isFamilyTraveler,
        IReadOnlyDictionary<Guid, decimal>? seasonalityMultipliers,
        IReadOnlyList<HolidayBoostRule>? activeHolidayRules,
        int userShareCount)
    {
        var distanceKm = DistanceKm(source, candidate);
        if (distanceKm is > MaxDistanceKm)
            return null;

        var categoryMatch = CategoryMatch(source, candidate);
        var priceSimilarity = PriceSimilarity(source.BasePriceAmount, candidate.BasePriceAmount);
        var proximityScore = ProximityScore(distanceKm);
        var normalizedRating = Math.Clamp(candidate.AverageRating / 5m, 0m, 1m);
        var popularity = Math.Clamp((decimal)(Math.Log10(candidate.BookingCount + 1d) / 4d), 0m, 1m);
        var featured = candidate.IsFeatured ? 0.1m : 0m;

        var score =
            weights.Category * categoryMatch +
            weights.Price * priceSimilarity +
            weights.Proximity * proximityScore +
            weights.Rating * normalizedRating +
            weights.Popularity * popularity +
            weights.Featured * featured;

        // 3.4: Recency boost
        var recencyMultiplier = RecencyMultiplier(candidate.CreatedAt, now);
        score *= recencyMultiplier;

        // 3.2: Inventory awareness (occupancy factor)
        var occupancyFactor = OccupancyFactor(candidate.UpcomingCapacity, candidate.UpcomingBookings);
        score *= occupancyFactor;

        // 3.6: Boost package (linear decay)
        var isBoosted = false;
        if (activeBoosts is not null &&
            activeBoosts.TryGetValue((candidate.EntityKind, candidate.EntityId), out var boost))
        {
            var boostMultiplier = boost.ComputeDecayedMultiplier(now);
            if (boostMultiplier > 1m)
            {
                score *= boostMultiplier;
                isBoosted = true;
            }
        }

        // 4.5: Family traveler boost
        var isFamilyBoosted = false;
        if (isFamilyTraveler && candidate.IsChildFriendly)
        {
            score *= 1.3m;
            isFamilyBoosted = true;
        }

        // 5.1: Seasonality boost
        var isSeasonBoosted = false;
        if (seasonalityMultipliers is not null && candidate.PlaceId is { } candPlaceId &&
            seasonalityMultipliers.TryGetValue(candPlaceId, out var seasonMultiplier) && seasonMultiplier != 1.0m)
        {
            score *= seasonMultiplier;
            isSeasonBoosted = true;
        }

        // 5.2: Holiday boost rules
        var isHolidayBoosted = false;
        if (activeHolidayRules is { Count: > 0 })
        {
            foreach (var rule in activeHolidayRules)
            {
                if (MatchesHolidayFilter(candidate, rule.Filter))
                {
                    score *= rule.Multiplier;
                    isHolidayBoosted = true;
                }
            }
        }

        // 5.5: Photogenic boost for frequent sharers
        var isPhotogenicBoosted = false;
        if (candidate.IsPhotogenicHotspot && userShareCount > 5)
        {
            score *= 1.4m;
            isPhotogenicBoosted = true;
        }

        var signals = BuildSignals(categoryMatch, priceSimilarity, proximityScore, normalizedRating, popularity, featured,
            recencyMultiplier, occupancyFactor, isBoosted, isFamilyBoosted, isSeasonBoosted, isHolidayBoosted, isPhotogenicBoosted);
        return new ScoredCandidate(candidate, Math.Round(score, 6, MidpointRounding.AwayFromZero), signals, isBoosted);
    }

    /// <summary>Matches a candidate against holiday filter rules.</summary>
    private static bool MatchesHolidayFilter(EntityAttributeSnapshot candidate, string filter)
    {
        return filter.ToLowerInvariant() switch
        {
            "familysuitable" => candidate.IsChildFriendly,
            "alcoholrelated" => candidate.HasAlcoholFreeArea == false,
            "outdooractivity" => candidate.Difficulty is not null,
            "eveningtour" => candidate.DurationMinutes is not null, // placeholder — refine when time-of-day data available
            _ => true // unknown filter matches all
        };
    }

    /// <summary>
    /// MMR selection: Score_MMR = λ × Relevance(d) − (1−λ) × max_sim(d, S)
    /// where max_sim uses Jaccard similarity on category overlap.
    /// Also enforces same-provider cap (max 2 per PlaceId).
    /// </summary>
    private static IReadOnlyList<ScoredCandidate> ApplyMmrDiversity(IReadOnlyList<ScoredCandidate> scored, int maxResults, decimal lambda)
    {
        if (scored.Count == 0)
            return scored;

        var selected = new List<ScoredCandidate>(maxResults);
        var selectedCategories = new List<HashSet<Guid>>(maxResults);
        var placeCounts = new Dictionary<Guid, int>();
        var remaining = new List<ScoredCandidate>(scored);

        // Normalize relevance scores to [0,1]
        var maxScore = remaining.Max(c => c.Score);
        var minScore = remaining.Min(c => c.Score);
        var scoreRange = maxScore - minScore;

        while (selected.Count < maxResults && remaining.Count > 0)
        {
            ScoredCandidate? best = null;
            var bestMmr = decimal.MinValue;
            var bestIndex = -1;

            for (var i = 0; i < remaining.Count; i++)
            {
                var candidate = remaining[i];

                // Same-provider cap
                if (candidate.Snapshot.PlaceId is { } placeId && placeCounts.GetValueOrDefault(placeId) >= MaxPerPlaceId)
                    continue;

                var normalizedRelevance = scoreRange > 0
                    ? (candidate.Score - minScore) / scoreRange
                    : 1m;

                var maxSimilarity = MaxCategorySimilarity(candidate.Snapshot, selectedCategories);

                var mmrScore = lambda * normalizedRelevance - (1m - lambda) * maxSimilarity;

                if (mmrScore > bestMmr)
                {
                    bestMmr = mmrScore;
                    best = candidate;
                    bestIndex = i;
                }
            }

            if (best is null)
                break;

            selected.Add(best);
            selectedCategories.Add(ParseCategoryIds(best.Snapshot.CategoryIdsJson));

            if (best.Snapshot.PlaceId is { } selectedPlaceId)
                placeCounts[selectedPlaceId] = placeCounts.GetValueOrDefault(selectedPlaceId) + 1;

            remaining.RemoveAt(bestIndex);
        }

        return selected;
    }

    /// <summary>Jaccard similarity between candidate's categories and the most similar already-selected item.</summary>
    private static decimal MaxCategorySimilarity(EntityAttributeSnapshot candidate, List<HashSet<Guid>> selectedSets)
    {
        if (selectedSets.Count == 0)
            return 0m;

        var candidateSet = ParseCategoryIds(candidate.CategoryIdsJson);
        if (candidateSet.Count == 0)
            return 0m;

        var maxSim = 0m;
        foreach (var selectedSet in selectedSets)
        {
            if (selectedSet.Count == 0)
                continue;

            var intersection = candidateSet.Count(selectedSet.Contains);
            var union = candidateSet.Count + selectedSet.Count - intersection;
            if (union > 0)
            {
                var sim = (decimal)intersection / union;
                if (sim > maxSim)
                    maxSim = sim;
            }
        }

        return maxSim;
    }

    // 3.3: Negative review suppression — exclude if rating < 3.0 AND 5+ reviews
    private static bool IsNegativeReviewed(EntityAttributeSnapshot snapshot)
        => snapshot.AverageRating < 3.0m && snapshot.ReviewCount >= 5;

    // 3.4: Recency boost multiplier
    private static decimal RecencyMultiplier(DateTime createdAt, DateTime now)
    {
        var age = now - createdAt;
        if (age.TotalDays <= 7) return 1.30m;
        if (age.TotalDays <= 14) return 1.20m;
        if (age.TotalDays <= 30) return 1.10m;
        return 1.0m;
    }

    // 3.2: Occupancy factor — deprioritize almost sold out
    private static decimal OccupancyFactor(int? upcomingCapacity, int? upcomingBookings)
    {
        if (upcomingCapacity is null or <= 0 || upcomingBookings is null)
            return 1.0m;

        var ratio = (decimal)upcomingBookings.Value / upcomingCapacity.Value;
        if (ratio > 0.9m) return 0.5m;
        if (ratio > 0.7m) return 0.8m;
        return 1.0m;
    }

    // 3.5: Check if Accept-Language indicates Arabic
    private static bool IsArabicLocale(string? acceptLanguage)
        => !string.IsNullOrWhiteSpace(acceptLanguage) && acceptLanguage.StartsWith("ar", StringComparison.OrdinalIgnoreCase);

    private static decimal CategoryMatch(EntityAttributeSnapshot source, EntityAttributeSnapshot candidate)
    {
        var sourceCategories = ParseCategoryIds(source.CategoryIdsJson);
        if (sourceCategories.Count == 0)
            return 0m;

        return ParseCategoryIds(candidate.CategoryIdsJson).Any(sourceCategories.Contains) ? 1m : 0m;
    }

    private static decimal PriceSimilarity(decimal? sourcePrice, decimal? candidatePrice)
    {
        if (sourcePrice is null && candidatePrice is null)
            return 0.5m;

        if (sourcePrice is null || candidatePrice is null || sourcePrice <= 0m || candidatePrice <= 0m)
            return 0m;

        var max = Math.Max(sourcePrice.Value, candidatePrice.Value);
        return Math.Clamp(1m - Math.Abs(sourcePrice.Value - candidatePrice.Value) / max, 0m, 1m);
    }

    private static decimal ProximityScore(double? distanceKm)
        => distanceKm is null ? 0m : (decimal)(1d / (1d + distanceKm.Value / 10d));

    private static double? DistanceKm(EntityAttributeSnapshot source, EntityAttributeSnapshot candidate)
    {
        if (source.LocationLatitude is null || source.LocationLongitude is null || candidate.LocationLatitude is null || candidate.LocationLongitude is null)
            return null;

        var sourceLocation = new Location(source.LocationLatitude.Value, source.LocationLongitude.Value);
        var candidateLocation = new Location(candidate.LocationLatitude.Value, candidate.LocationLongitude.Value);
        return sourceLocation.DistanceTo(candidateLocation);
    }

    internal static HashSet<Guid> ParseCategoryIds(string? categoryIdsJson)
    {
        if (string.IsNullOrWhiteSpace(categoryIdsJson))
            return [];

        try
        {
            return JsonSerializer.Deserialize<Guid[]>(categoryIdsJson) is { } ids ? ids.ToHashSet() : [];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private static IReadOnlyList<string> BuildSignals(
        decimal categoryMatch, decimal priceSimilarity, decimal proximityScore,
        decimal normalizedRating, decimal popularity, decimal featured,
        decimal recencyMultiplier, decimal occupancyFactor, bool isBoosted, bool isFamilyBoosted,
        bool isSeasonBoosted, bool isHolidayBoosted, bool isPhotogenicBoosted)
    {
        var signals = new List<string>(15);
        if (categoryMatch > 0m) signals.Add("category-match");
        if (priceSimilarity >= 0.75m) signals.Add("similar-price");
        if (proximityScore >= 0.5m) signals.Add("nearby");
        if (normalizedRating >= 0.8m) signals.Add("high-rating");
        if (popularity >= 0.25m) signals.Add("popular");
        if (featured > 0m) signals.Add("featured");
        if (recencyMultiplier > 1.0m) signals.Add("new-listing");
        if (occupancyFactor < 1.0m) signals.Add("high-demand");
        if (isBoosted) signals.Add("boosted");
        if (isFamilyBoosted) signals.Add("kid-friendly");
        if (isSeasonBoosted) signals.Add("seasonal");
        if (isHolidayBoosted) signals.Add("holiday-special");
        if (isPhotogenicBoosted) signals.Add("photo-worthy");
        return signals;
    }

    private sealed record WeightSet(decimal Category, decimal Price, decimal Proximity, decimal Rating, decimal Popularity, decimal Featured);
}
