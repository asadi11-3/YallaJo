namespace ContentTours.Application.Queries.Tour.SearchTours;

public sealed record PriceBucket(decimal Min, decimal? Max, int Count);
public sealed record RatingBucket(decimal Min, decimal Max, int Count);

public sealed record SearchFacets(
    IReadOnlyList<PriceBucket> PriceBuckets,
    IReadOnlyDictionary<string, int> DifficultyCounts,
    IReadOnlyList<RatingBucket> RatingBuckets,
    int IsChildFriendlyCount,
    int IsAccessibleCount,
    int IsInstantBookingCount);
