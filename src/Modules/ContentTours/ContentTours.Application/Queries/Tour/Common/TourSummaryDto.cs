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
    DateTime CreatedAt);
