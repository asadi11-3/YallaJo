namespace ContentPlaces.Application.Queries.Business.Common;

public sealed record BusinessSummaryDto(
    Guid Id,
    string Name,
    string Slug,
    string BusinessType,
    string Status,
    double Lat,
    double Lng,
    string? City,
    string? Country,
    decimal AverageRating,
    int ReviewCount,
    bool IsVerified,
    bool IsFeatured,
    string? PrimaryImageUrl);
