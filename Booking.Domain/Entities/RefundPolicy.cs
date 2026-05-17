using YallaJo.SharedKernel.Domain.Entities;

namespace Booking.Domain.Entities;

public sealed class RefundPolicy : AuditableEntity, IAggregateRoot
{
    private RefundPolicy() { } // EF Core

    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public int FullRefundHours { get; private set; }
    public int PartialRefundHours { get; private set; }
    public decimal PartialRefundPercent { get; private set; }
    public bool IsDefault { get; private set; }
    public bool IsActive { get; private set; } = true;
}
