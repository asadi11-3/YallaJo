namespace YallaJo.Web.Areas.Guide.Models.Earnings;

/// <summary>
/// One row of the guide's earnings broken down per tour.
/// Mirrors the backend GuideEarningByTour contract.
/// </summary>
public sealed record GuideEarningByTourResponse(
    Guid TourId,
    string TourName,
    int BookingCount,
    decimal GrossAmount,
    decimal CommissionAmount,
    decimal NetAmount,
    string Currency);

/// <summary>
/// One row of the guide's earnings history (per booking/earning).
/// Mirrors the backend GuideEarningHistoryItem contract.
/// </summary>
public sealed record GuideEarningHistoryItemResponse(
    Guid EarningId,
    Guid BookingId,
    DateOnly EarnedDate,
    decimal GrossAmount,
    decimal CommissionAmount,
    decimal NetAmount,
    string Currency,
    string Status);

/// <summary>
/// Web-side wrapper matching the backend SharedKernel PaginatedResult&lt;T&gt; JSON
/// shape (Items / PageNumber / PageSize / TotalCount).
/// </summary>
public sealed class PaginatedResponse<T>
{
    public IReadOnlyList<T> Items { get; init; } = [];
    public int PageNumber { get; init; }
    public int PageSize { get; init; }
    public int TotalCount { get; init; }
}
