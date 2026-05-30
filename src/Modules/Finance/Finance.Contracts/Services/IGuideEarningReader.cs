using YallaJo.SharedKernel.Domain.Abstractions.Pagination;

namespace Finance.Contracts.Services;

public sealed record GuideEarningsSummary(
    decimal TotalEarned,
    decimal ThisMonth,
    decimal PendingPayout,
    decimal CommissionDeducted,
    decimal NetEarnings,
    string Currency);

public sealed record GuideEarningByTour(
    Guid TourId,
    string TourName,
    int BookingCount,
    decimal GrossAmount,
    decimal CommissionAmount,
    decimal NetAmount,
    string Currency);

public sealed record GuideEarningHistoryItem(
    Guid EarningId,
    Guid BookingId,
    DateOnly EarnedDate,
    decimal GrossAmount,
    decimal CommissionAmount,
    decimal NetAmount,
    string Currency,
    string Status);

public interface IGuideEarningReader
{
    Task<GuideEarningsSummary> GetSummaryAsync(Guid guideUserId, CancellationToken ct = default);
    Task<IReadOnlyList<GuideEarningByTour>> GetByTourAsync(Guid guideUserId, CancellationToken ct = default);
    Task<PaginatedResult<GuideEarningHistoryItem>> GetHistoryAsync(Guid guideUserId, int page, int pageSize, CancellationToken ct = default);
}
