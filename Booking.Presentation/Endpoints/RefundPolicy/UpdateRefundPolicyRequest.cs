using Booking.Application.Commands.RefundPolicy.UpdateRefundPolicy;

namespace Booking.Presentation.Endpoints.RefundPolicy;

public sealed record UpdateRefundPolicyRequest(
    IReadOnlyList<RefundTierRequest> Tiers,
    string? RowVersion)
{
    public UpdateRefundPolicyCommand ToCommand(Guid policyId)
    {
        byte[] decodedRowVersion;
        try
        {
            decodedRowVersion = string.IsNullOrWhiteSpace(RowVersion)
                ? []
                : Convert.FromBase64String(RowVersion);
        }
        catch (FormatException)
        {
            decodedRowVersion = [];
        }

        return new UpdateRefundPolicyCommand(
            PolicyId: policyId,
            Tiers: (Tiers ?? Array.Empty<RefundTierRequest>())
                .Select(t => t.ToDto())
                .ToList(),
            RowVersion: decodedRowVersion);
    }
}
