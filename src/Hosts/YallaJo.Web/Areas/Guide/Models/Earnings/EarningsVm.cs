namespace YallaJo.Web.Areas.Guide.Models.Earnings;

public sealed class EarningsVm
{
    public decimal TotalEarned { get; init; }
    public decimal ThisMonth { get; init; }
    public decimal PendingPayout { get; init; }
    public decimal CommissionDeducted { get; init; }
    public decimal NetEarnings { get; init; }
    public string Currency { get; init; } = "JOD";

    public IReadOnlyList<EarningByTourRowVm> ByTour { get; init; } = [];
    public IReadOnlyList<EarningHistoryRowVm> History { get; init; } = [];

    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
    public int TotalCount { get; init; }

    public bool HasByTour => ByTour.Count > 0;
    public bool HasHistory => History.Count > 0;
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling(TotalCount / (double)PageSize) : 0;
    public bool HasPreviousPage => Page > 1;
    public bool HasNextPage => Page < TotalPages;
}

public sealed record EarningByTourRowVm(
    Guid TourId,
    string TourName,
    int BookingCount,
    decimal GrossAmount,
    decimal CommissionAmount,
    decimal NetAmount,
    string Currency);

public sealed record EarningHistoryRowVm(
    Guid EarningId,
    Guid BookingId,
    DateOnly EarnedDate,
    decimal GrossAmount,
    decimal CommissionAmount,
    decimal NetAmount,
    string Currency,
    string Status);
