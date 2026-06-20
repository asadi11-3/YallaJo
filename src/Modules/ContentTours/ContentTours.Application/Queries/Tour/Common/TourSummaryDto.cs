namespace ContentTours.Application.Queries.Tour.Common;

public sealed record TourSummaryDto(
    Guid Id,
    string Name,
    string Slug,
    decimal BasePrice,
    string Currency,
    decimal? SalePrice,
    decimal AverageRating,
    int ReviewCount,
    int BookingCount,
    bool IsFeatured,
    string Status,
    DateTime CreatedAt,
    // Real primary tour image URL (relative /uploads path) for public card surfaces,
    // batch-loaded from ContentCore EntityImages after paging. Null when the tour has
    // no images or on non-card paths (Featured/MyTours/Package) that don't enrich it;
    // the Web layer falls back to a deterministic placeholder when null.
    string? PrimaryImageUrl = null);
