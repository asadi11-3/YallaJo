namespace Booking.Application.Queries.GetRefundPolicyByTour;

public sealed record RefundTierDto(int HoursBeforeTour, decimal RefundPercent);
