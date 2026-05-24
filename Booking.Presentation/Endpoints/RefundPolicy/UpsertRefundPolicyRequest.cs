using Booking.Application.Commands.RefundPolicy.UpsertRefundPolicy;

namespace Booking.Presentation.Endpoints.RefundPolicy;

public sealed record UpsertRefundPolicyRequest(
    Guid TourId,
    IReadOnlyList<RefundTierRequest> Tiers)
{
    public UpsertRefundPolicyCommand ToCommand()
        => new(
            TourId: TourId,
            Tiers: (Tiers ?? Array.Empty<RefundTierRequest>())
                .Select(t => t.ToDto())
                .ToList());
}
