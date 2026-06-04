namespace YallaJo.Web.Areas.Provider.Models.Dashboard;

public sealed class ListMyToursResponse
{
    public IReadOnlyList<TourSummaryResponse> Items { get; init; } = [];
    public int Total { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalPages { get; init; }
}

public sealed class TourSummaryResponse
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Slug { get; init; } = string.Empty;
    public decimal BasePrice { get; init; }
    public string Currency { get; init; } = string.Empty;
    public decimal? SalePrice { get; init; }
    public decimal AverageRating { get; init; }
    public int ReviewCount { get; init; }
    public int BookingCount { get; init; }
    public bool IsFeatured { get; init; }
    public string? Status { get; init; }
    public DateTime CreatedAt { get; init; }
}

public sealed class GuideEarningsSummaryResponse
{
    public decimal GrossTotal { get; init; }
    public decimal NetEstimateTotal { get; init; }
    public int PaymentCount { get; init; }
    public string Currency { get; init; } = string.Empty;
}

public sealed class JoinRequestResponse
{
    public Guid Id { get; init; }
    public Guid TourBookingId { get; init; }
    public Guid UserId { get; init; }
    public string Status { get; init; } = string.Empty;
    public int ParticipantCount { get; init; }
    public string? Message { get; init; }
    public DateTime ExpiresAt { get; init; }
    public DateTime? RespondedAt { get; init; }
    public string? ResponseMessage { get; init; }
    public Guid? ResultingBookingId { get; init; }
    public DateTime CreatedAt { get; init; }
}
