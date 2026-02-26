using YallaJo.SharedKernel.Domain.Entities;
using YallaJo.SharedKernel.Domain.ValueObjects;

namespace Finance.Domain.Entities;

public sealed class PayoutItem : BaseEntity
{
    private PayoutItem() { } // EF Core

    public Guid PayoutId { get; private set; }
    public Guid BookingId { get; private set; }
    public Money Amount { get; private set; } = default!;
    public Money Commission { get; private set; } = default!;
    public Money NetAmount { get; private set; } = default!;

    public Payout Payout { get; private set; } = default!;
}
