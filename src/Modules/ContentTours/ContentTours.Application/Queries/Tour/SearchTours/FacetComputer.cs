namespace ContentTours.Application.Queries.Tour.SearchTours;

internal static class FacetComputer
{
    private static readonly RatingBucket[] RatingBuckets =
    [
        new(4.5m, 5.0m, 0),
        new(4.0m, 4.5m, 0),
        new(3.0m, 4.0m, 0),
        new(0.0m, 3.0m, 0),
    ];

    public static SearchFacets Compute(IReadOnlyList<FacetRow> rows)
    {
        if (rows.Count == 0)
            return new SearchFacets([], new Dictionary<string, int>(), [], 0, 0, 0);

        // Price buckets — dynamic 5 equal-width buckets
        var minPrice = rows.Min(r => r.BasePrice);
        var maxPrice = rows.Max(r => r.BasePrice);
        var priceBuckets = BuildPriceBuckets(rows, minPrice, maxPrice);

        // Difficulty counts
        var difficultyCounts = rows
            .GroupBy(r => r.Difficulty)
            .ToDictionary(g => g.Key, g => g.Count());

        // Rating buckets (fixed ranges)
        var ratingBuckets = RatingBuckets
            .Select(rb => rb with
            {
                Count = rows.Count(r => r.AverageRating >= rb.Min && r.AverageRating < rb.Max)
            })
            .ToList() as IReadOnlyList<RatingBucket>;

        return new SearchFacets(
            priceBuckets,
            difficultyCounts,
            ratingBuckets,
            rows.Count(r => r.IsChildFriendly),
            rows.Count(r => r.IsAccessible),
            rows.Count(r => r.IsInstantBooking));
    }

    private static IReadOnlyList<PriceBucket> BuildPriceBuckets(
        IReadOnlyList<FacetRow> rows, decimal min, decimal max)
    {
        if (min == max)
            return [new PriceBucket(min, null, rows.Count)];

        var width = (max - min) / 5;
        var buckets = new List<PriceBucket>();
        for (int i = 0; i < 5; i++)
        {
            var lo = min + i * width;
            var hi = i == 4 ? (decimal?)null : lo + width;
            var count = rows.Count(r =>
                r.BasePrice >= lo && (hi == null || r.BasePrice < hi.Value));
            buckets.Add(new PriceBucket(Math.Round(lo, 2), hi.HasValue ? Math.Round(hi.Value, 2) : null, count));
        }
        return buckets;
    }
}
