using YallaJo.SharedKernel.Domain.Entities;
using YallaJo.SharedKernel.Domain.ValueObjects;

namespace Finance.Domain.Entities;

public sealed class DiscountUsage : BaseEntity
{
    private DiscountUsage() { } // EF Core

    public Guid DiscountId { get; private set; }
    public Guid UserId { get; private set; }
    public Guid? BookingId { get; private set; }
    public Money Amount { get; private set; } = default!;
    public DateTime UsedAt { get; private set; }

    public Discount Discount { get; private set; } = default!;
}
