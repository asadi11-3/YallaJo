namespace YallaJo.Web.Areas.Public.Models.Places;

public sealed class PlaceAccessibilityFeatureResponse
{
    public Guid Id { get; init; }
    public string FeatureType { get; init; } = "";
    public string Name { get; init; } = "";
    public string? Description { get; init; }
    public bool IsAvailable { get; init; }
}

public sealed class PlaceAccessibilityCatalogItemResponse
{
    public string FeatureType { get; init; } = "";
    public string Code { get; init; } = "";
    public string DisplayName { get; init; } = "";
}

public sealed class RecommendationListResponse
{
    public IReadOnlyList<RecommendationItemResponse> Items { get; init; } = [];
}

public sealed class RecommendationItemResponse
{
    public RecommendationEntityKind Kind { get; init; }
    public Guid Id { get; init; }
    public string Name { get; init; } = "";
    public string Slug { get; init; } = "";
    public decimal? BasePrice { get; init; }
    public string? Currency { get; init; }
    public decimal AverageRating { get; init; }
    public int BookingCount { get; init; }
    public decimal Score { get; init; }
    public IReadOnlyList<string> Signals { get; init; } = [];
    public bool IsFeatured { get; init; }
    public bool IsPinned { get; init; }
    public string? BadgeText { get; init; }
    public bool IsBoosted { get; init; }
}

public enum RecommendationEntityKind : byte
{
    Tour = 1,
    Place = 2,
    Business = 3,
    Category = 4,
}

public sealed record SponsoredClickBody(Guid BidId, string SourceKind, Guid SourceId, int Position, int DwellTimeSeconds);
