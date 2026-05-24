namespace Booking.Application.Queries.GetRefundPolicyByTour;

public sealed record RefundPolicyDto(
    Guid Id,
    Guid TourId,
    IReadOnlyList<RefundTierDto> Tiers,
    bool IsActive,
    bool IsDefault,
    string RowVersion);
