using YallaJo.SharedKernel.Domain.Entities;

namespace Finance.Domain.Entities;

public sealed class PayoutItem : BaseEntity
{
    private PayoutItem() { } // EF Core

    public Guid PayoutId { get; private set; }
    public Guid BookingId { get; private set; }
    public decimal Amount { get; private set; }
    public decimal Commission { get; private set; }
    public decimal NetAmount { get; private set; }

    public Payout Payout { get; private set; } = default!;
}
