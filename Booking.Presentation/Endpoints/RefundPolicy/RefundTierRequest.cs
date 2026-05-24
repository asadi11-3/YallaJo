using Booking.Application.Queries.GetRefundPolicyByTour;

namespace Booking.Presentation.Endpoints.RefundPolicy;

public sealed record RefundTierRequest(int HoursBeforeTour, decimal RefundPercent)
{
    public RefundTierDto ToDto() => new(HoursBeforeTour, RefundPercent);
}
