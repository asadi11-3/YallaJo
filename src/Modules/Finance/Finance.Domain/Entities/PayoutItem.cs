using YallaJo.SharedKernel.Domain.Entities;
using YallaJo.SharedKernel.Domain.ValueObjects;

namespace Finance.Domain.Entities;

/// <summary>
/// Per-booking accounting line under a Payout aggregate.
/// </summary>
public sealed class PayoutItem : BaseEntity
{
    private PayoutItem() { } // EF Core

    internal PayoutItem(
        Guid payoutId,
        Guid bookingId,
        Money grossAmount,
        Money commissionAmount,
        Money netAmount,
        Guid? commissionRuleSnapshotId)
    {
        if (payoutId == Guid.Empty)
        {
            throw new ArgumentException("PayoutId is required.", nameof(payoutId));
        }

        if (bookingId == Guid.Empty)
        {
            throw new ArgumentException("BookingId is required.", nameof(bookingId));
        }

        PayoutId = payoutId;
        BookingId = bookingId;
        GrossAmount = grossAmount ?? throw new ArgumentNullException(nameof(grossAmount));
        CommissionAmount = commissionAmount ?? throw new ArgumentNullException(nameof(commissionAmount));
        NetAmount = netAmount ?? throw new ArgumentNullException(nameof(netAmount));
        CommissionRuleSnapshotId = commissionRuleSnapshotId;
    }

    public Guid PayoutId { get; private set; }
    public Guid BookingId { get; private set; }
    public Money GrossAmount { get; private set; } = default!;
    public Money CommissionAmount { get; private set; } = default!;
    public Money NetAmount { get; private set; } = default!;
    public Guid? CommissionRuleSnapshotId { get; private set; }

    public Payout Payout { get; private set; } = default!;
}
