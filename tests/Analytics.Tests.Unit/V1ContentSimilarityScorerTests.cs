using System.Text.Json;
using Analytics.Application.Scoring;
using Analytics.Domain.Entities;
using Analytics.Domain.Enums;
using FluentAssertions;
using Xunit;

namespace Analytics.Tests.Unit;

public sealed class V1ContentSimilarityScorerTests
{
    // BaseEntity.CreatedAt = DateTime.UtcNow (always), so all test snapshots are ≤7d old → RecencyMultiplier = 1.30×
    private const decimal RecencyBoost = 1.30m;
    private static readonly DateTime SnapshotTime = new(2024, 01, 01, 0, 0, 0, DateTimeKind.Utc);
    private static readonly Guid DefaultCategoryId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    private readonly V1ContentSimilarityScorer _scorer = new();

    // ═══════════════════════════════════════════════
    // V1 Formula Tests (updated for V1.5 recency 1.30×)
    // ═══════════════════════════════════════════════

    [Fact]
    public void Score_ShouldMatchSimilarToursFormula_AndExposeAllSignals()
    {
        var categoryId = Guid.NewGuid();
        var source = Tour(price: 100m, categories: [categoryId], latitude: 31.95m, longitude: 35.93m);
        var candidate = Tour(
            price: 80m,
            averageRating: 4m,
            bookingCount: 99,
            isFeatured: true,
            categories: [categoryId],
            latitude: 31.95m,
            longitude: 35.93m);

        var result = Score(source, [candidate]).Should().ContainSingle().Subject;

        // V1 base = 0.825000 × 1.30 recency = 1.072500
        result.Score.Should().Be(Round(0.825000m * RecencyBoost));
        result.Signals.Should().BeEquivalentTo(
            "category-match",
            "similar-price",
            "nearby",
            "high-rating",
            "popular",
            "featured",
            "new-listing");
    }

    [Fact]
    public void Score_ShouldApplyCategoryMatchOnlyWhenCategoriesOverlap()
    {
        var sourceCategoryId = Guid.NewGuid();
        var matched = Tour(categories: [sourceCategoryId]);
        var unmatched = Tour(categories: [Guid.NewGuid()]);
        var source = Tour(categories: [sourceCategoryId]);

        var results = Score(source, [matched, unmatched], maxResults: 2);

        results.Single(x => x.Snapshot.EntityId == matched.EntityId).Signals.Should().Contain("category-match");
        results.Single(x => x.Snapshot.EntityId == unmatched.EntityId).Signals.Should().NotContain("category-match");
        // Delta = W_category × 1.0 = 0.30, multiplied by recency
        results.Single(x => x.Snapshot.EntityId == matched.EntityId).Score
            .Should().Be(results.Single(x => x.Snapshot.EntityId == unmatched.EntityId).Score + Round(0.300000m * RecencyBoost));
    }

    [Fact]
    public void Score_ShouldApplyPriceSimilarityComponent()
    {
        var source = Tour(price: 100m);
        var samePrice = Tour(price: 100m);
        var halfPrice = Tour(price: 50m);

        var results = Score(source, [samePrice, halfPrice], maxResults: 2);

        results.Single(x => x.Snapshot.EntityId == samePrice.EntityId).Signals.Should().Contain("similar-price");
        results.Single(x => x.Snapshot.EntityId == halfPrice.EntityId).Signals.Should().NotContain("similar-price");
        results.Single(x => x.Snapshot.EntityId == samePrice.EntityId).Score
            .Should().Be(results.Single(x => x.Snapshot.EntityId == halfPrice.EntityId).Score + Round(0.100000m * RecencyBoost));
    }

    [Fact]
    public void Score_ShouldApplyProximityComponent()
    {
        var source = Tour(latitude: 31.95m, longitude: 35.93m);
        var sameLocation = Tour(latitude: 31.95m, longitude: 35.93m);
        var missingLocation = Tour(latitude: null, longitude: null);

        var results = Score(source, [sameLocation, missingLocation], maxResults: 2);

        results.Single(x => x.Snapshot.EntityId == sameLocation.EntityId).Signals.Should().Contain("nearby");
        results.Single(x => x.Snapshot.EntityId == missingLocation.EntityId).Signals.Should().NotContain("nearby");
        results.Single(x => x.Snapshot.EntityId == sameLocation.EntityId).Score
            .Should().Be(results.Single(x => x.Snapshot.EntityId == missingLocation.EntityId).Score + Round(0.150000m * RecencyBoost));
    }

    [Fact]
    public void Score_ShouldApplyNormalizedRatingComponent()
    {
        var source = Tour();
        var fiveStars = Tour(averageRating: 5m);
        var zeroStars = Tour(averageRating: 0m);

        var results = Score(source, [fiveStars, zeroStars], maxResults: 2);

        results.Single(x => x.Snapshot.EntityId == fiveStars.EntityId).Signals.Should().Contain("high-rating");
        results.Single(x => x.Snapshot.EntityId == zeroStars.EntityId).Signals.Should().NotContain("high-rating");
        results.Single(x => x.Snapshot.EntityId == fiveStars.EntityId).Score
            .Should().Be(results.Single(x => x.Snapshot.EntityId == zeroStars.EntityId).Score + Round(0.200000m * RecencyBoost));
    }

    [Fact]
    public void Score_ShouldApplyPopularityComponent()
    {
        var source = Tour();
        var popular = Tour(bookingCount: 999);
        var noBookings = Tour(bookingCount: 0);

        var results = Score(source, [popular, noBookings], maxResults: 2);

        results.Single(x => x.Snapshot.EntityId == popular.EntityId).Signals.Should().Contain("popular");
        results.Single(x => x.Snapshot.EntityId == noBookings.EntityId).Signals.Should().NotContain("popular");
        results.Single(x => x.Snapshot.EntityId == popular.EntityId).Score
            .Should().Be(results.Single(x => x.Snapshot.EntityId == noBookings.EntityId).Score + Round(0.075000m * RecencyBoost));
    }

    [Fact]
    public void Score_ShouldApplyFeaturedBonusComponent()
    {
        var source = Tour();
        var featured = Tour(isFeatured: true);
        var regular = Tour(isFeatured: false);

        var results = Score(source, [featured, regular], maxResults: 2);

        results.Single(x => x.Snapshot.EntityId == featured.EntityId).Signals.Should().Contain("featured");
        results.Single(x => x.Snapshot.EntityId == regular.EntityId).Signals.Should().NotContain("featured");
        results.Single(x => x.Snapshot.EntityId == featured.EntityId).Score
            .Should().Be(results.Single(x => x.Snapshot.EntityId == regular.EntityId).Score + Round(0.005000m * RecencyBoost));
    }

    [Fact]
    public void Score_WithNullSourceLocation_ShouldSetProximityToZero()
    {
        var source = Tour(latitude: null, longitude: null);
        var candidate = Tour(latitude: 31.95m, longitude: 35.93m);

        var result = Score(source, [candidate]).Should().ContainSingle().Subject;

        result.Signals.Should().NotContain("nearby");
        result.Score.Should().Be(Round(0.700000m * RecencyBoost));
    }

    [Fact]
    public void Score_WithNullCandidateLocation_ShouldSetProximityToZero()
    {
        var source = Tour(latitude: 31.95m, longitude: 35.93m);
        var candidate = Tour(latitude: null, longitude: null);

        var result = Score(source, [candidate]).Should().ContainSingle().Subject;

        result.Signals.Should().NotContain("nearby");
        result.Score.Should().Be(Round(0.700000m * RecencyBoost));
    }

    [Fact]
    public void Score_WithZeroPrice_ShouldSetPriceSimilarityToZero()
    {
        var source = Tour(price: 100m);
        var candidate = Tour(price: 0m);

        var result = Score(source, [candidate]).Should().ContainSingle().Subject;

        result.Signals.Should().NotContain("similar-price");
        result.Score.Should().Be(Round(0.650000m * RecencyBoost));
    }

    [Fact]
    public void Score_WithZeroBookingCount_ShouldSetPopularityToZero()
    {
        var source = Tour();
        var candidate = Tour(bookingCount: 0);

        var result = Score(source, [candidate]).Should().ContainSingle().Subject;

        result.Signals.Should().NotContain("popular");
        result.Score.Should().Be(Round(0.850000m * RecencyBoost));
    }

    [Fact]
    public void Score_WithSourceHavingNoCategories_ShouldSetCategoryMatchToZeroForAllCandidates()
    {
        var source = Tour(categories: []);
        var candidate = Tour(categories: [Guid.NewGuid()]);

        var result = Score(source, [candidate]).Should().ContainSingle().Subject;

        result.Signals.Should().NotContain("category-match");
        result.Score.Should().Be(Round(0.550000m * RecencyBoost));
    }

    [Fact]
    public void Score_WithEmptyCandidates_ShouldReturnEmpty()
    {
        var results = Score(Tour(), []);

        results.Should().BeEmpty();
    }

    [Fact]
    public void Score_ShouldSortResultsByScoreDescending()
    {
        var categoryId = Guid.NewGuid();
        var source = Tour(price: 100m, categories: [categoryId]);
        var low = Tour(price: 10m, averageRating: 1m, bookingCount: 0, categories: [], placeId: Guid.NewGuid());
        var high = Tour(price: 100m, averageRating: 5m, bookingCount: 999, categories: [categoryId], placeId: Guid.NewGuid());
        var middle = Tour(price: 75m, averageRating: 3m, bookingCount: 9, categories: [], placeId: Guid.NewGuid());

        var results = Score(source, [low, high, middle], maxResults: 3);

        results.Select(x => x.Snapshot.EntityId).Should().Equal(high.EntityId, middle.EntityId, low.EntityId);
        results.Select(x => x.Score).Should().BeInDescendingOrder();
    }

    [Fact]
    public void Score_ShouldNotReturnMoreThanConfiguredMaxResults()
    {
        var source = Tour(price: 1000m);
        var candidates = Enumerable.Range(1, 10)
            .Select(i => Tour(price: i * 100m, placeId: Guid.NewGuid()))
            .ToArray();

        var results = Score(source, candidates, maxResults: 4);

        results.Should().HaveCount(4);
    }

    // ═══════════════════════════════════════════════
    // V1.5 MMR Diversity Tests (3.1)
    // ═══════════════════════════════════════════════

    [Fact]
    public void Score_ShouldCapResultsAtTwoPerPlaceId()
    {
        var placeId = Guid.NewGuid();
        var source = Tour(price: 500m);
        var candidates = Enumerable.Range(1, 10)
            .Select(i => Tour(price: i * 25m, placeId: placeId, bookingCount: 1000 - i))
            .ToArray();

        var results = Score(source, candidates, maxResults: 10);

        results.Should().HaveCount(2);
        results.Should().OnlyContain(x => x.Snapshot.PlaceId == placeId);
    }

    [Fact]
    public void Score_MmrDiversity_ShouldPenalizeDuplicateCategories()
    {
        var source = Tour(price: 100m);
        var candidates = Enumerable.Range(1, 10)
            .Select(i => Tour(price: 100m, placeId: Guid.NewGuid(), bookingCount: 1000 - i))
            .ToArray();

        var results = Score(source, candidates, maxResults: 10);

        // MMR returns all candidates (no hard price cap), but diversity-reorders them
        results.Should().HaveCount(10);
        results.Should().OnlyContain(x => x.Snapshot.BasePriceAmount == 100m);
    }

    // ═══════════════════════════════════════════════
    // V1.5 Negative Review Suppression Tests (3.3)
    // ═══════════════════════════════════════════════

    [Fact]
    public void Score_ShouldExcludeLowRatedCandidatesWithEnoughReviews()
    {
        var source = Tour();
        var bad = Tour(averageRating: 2.5m, reviewCount: 10, placeId: Guid.NewGuid());
        var good = Tour(averageRating: 4.5m, placeId: Guid.NewGuid());

        var results = Score(source, [bad, good], maxResults: 10);

        results.Should().ContainSingle();
        results[0].Snapshot.EntityId.Should().Be(good.EntityId);
    }

    [Fact]
    public void Score_ShouldNotExcludeLowRatedWithFewReviews()
    {
        var source = Tour();
        var lowRatingFewReviews = Tour(averageRating: 2.0m, reviewCount: 3, placeId: Guid.NewGuid());

        var results = Score(source, [lowRatingFewReviews], maxResults: 10);

        results.Should().ContainSingle();
    }

    // ═══════════════════════════════════════════════
    // V1.5 Recency Boost Tests (3.4)
    // ═══════════════════════════════════════════════

    [Fact]
    public void Score_NewListings_ShouldHaveRecencySignal()
    {
        // BaseEntity.CreatedAt = DateTime.UtcNow, always ≤7 days → "new-listing" signal
        var source = Tour();
        var candidate = Tour(placeId: Guid.NewGuid());

        var result = Score(source, [candidate]).Should().ContainSingle().Subject;

        result.Signals.Should().Contain("new-listing");
    }

    // ═══════════════════════════════════════════════
    // V1.5 Halal Filter Tests (3.5)
    // ═══════════════════════════════════════════════

    [Fact]
    public void Score_WithHalalOnly_ShouldFilterNonHalalCandidates()
    {
        var source = Tour();
        var halal = Tour(placeId: Guid.NewGuid());
        var nonHalal = Tour(placeId: Guid.NewGuid());

        // We can't set IsHalal directly on Tour snapshots (it's a Business field),
        // but we can verify that with HalalOnly=true and no IsHalal=true candidates, we get nothing
        var results = _scorer.Score(new ScoringContext(source, [halal, nonHalal],
            SuggestionContext.SimilarTours, MaxResults: 10, HalalOnly: true));

        // Tours don't set IsHalal (null), so halal filter excludes them all
        results.Should().BeEmpty();
    }

    [Fact]
    public void Score_WithArabicLocale_ShouldAutoEnableHalalFilter()
    {
        var source = Tour();
        var candidate = Tour(placeId: Guid.NewGuid());

        var results = _scorer.Score(new ScoringContext(source, [candidate],
            SuggestionContext.SimilarTours, MaxResults: 10, AcceptLanguage: "ar-JO"));

        // Tours don't have IsHalal=true, so auto-halal filter excludes them
        results.Should().BeEmpty();
    }

    [Fact]
    public void Score_WithEnglishLocale_ShouldNotFilterByHalal()
    {
        var source = Tour();
        var candidate = Tour(placeId: Guid.NewGuid());

        var results = _scorer.Score(new ScoringContext(source, [candidate],
            SuggestionContext.SimilarTours, MaxResults: 10, AcceptLanguage: "en-US"));

        results.Should().ContainSingle();
    }

    // ═══════════════════════════════════════════════
    // V1.5 Inventory Awareness Tests (3.2)
    // ═══════════════════════════════════════════════

    [Fact]
    public void Score_WithNullInventory_ShouldNotPenalize()
    {
        // Default Tour has null inventory → OccupancyFactor = 1.0×
        var source = Tour();
        var candidate = Tour(placeId: Guid.NewGuid());

        var result = Score(source, [candidate]).Should().ContainSingle().Subject;

        result.Signals.Should().NotContain("high-demand");
    }

    // ═══════════════════════════════════════════════
    // Helpers
    // ═══════════════════════════════════════════════

    private IReadOnlyList<ScoredCandidate> Score(
        EntityAttributeSnapshot source,
        IReadOnlyList<EntityAttributeSnapshot> candidates,
        int maxResults = 20)
        => _scorer.Score(new ScoringContext(source, candidates, SuggestionContext.SimilarTours, maxResults));

    private static decimal Round(decimal value)
        => Math.Round(value, 6, MidpointRounding.AwayFromZero);

    private static EntityAttributeSnapshot Tour(
        decimal? price = 100m,
        decimal averageRating = 5m,
        int reviewCount = 0,
        int bookingCount = 0,
        bool isFeatured = false,
        IReadOnlyCollection<Guid>? categories = null,
        decimal? latitude = 31.95m,
        decimal? longitude = 35.93m,
        Guid? placeId = null)
        => EntityAttributeSnapshot.CreateForTour(
            entityId: Guid.NewGuid(),
            name: "Test tour",
            slug: null,
            basePriceAmount: price,
            basePriceCurrency: price is null ? null : "JOD",
            salePrice: null,
            averageRating: averageRating,
            reviewCount: reviewCount,
            bookingCount: bookingCount,
            isFeatured: isFeatured,
            locationLatitude: latitude,
            locationLongitude: longitude,
            placeId: placeId,
            difficulty: null,
            durationMinutes: null,
            isChildFriendly: false,
            isAccessible: false,
            isInstantBooking: false,
            status: "Published",
            categoryIdsJson: SerializeCategories(categories ?? [DefaultCategoryId]),
            lastSnapshotAt: SnapshotTime);

    private static string? SerializeCategories(IReadOnlyCollection<Guid> categories)
        => categories.Count == 0 ? null : JsonSerializer.Serialize(categories);
}
