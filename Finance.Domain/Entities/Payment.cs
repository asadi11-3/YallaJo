using Finance.Domain.Enums;
using YallaJo.SharedKernel.Domain.Entities;
using YallaJo.SharedKernel.Domain.ValueObjects;

namespace Finance.Domain.Entities;

public sealed class Payment : AuditableEntity, IAggregateRoot
{
    private readonly List<Dispute> _disputes = [];

    private Payment() { } // EF Core

    public Guid UserId { get; private set; }
    public Guid? BookingId { get; private set; }
    public Guid? ReservationId { get; private set; }
    public Money Amount { get; private set; } = default!;
    public string Currency { get; private set; } = string.Empty;
    public PaymentMethod PaymentMethod { get; private set; }
    public PaymentStatus Status { get; private set; } = PaymentStatus.Pending;
    public string? TransactionId { get; private set; }
    public string? GatewayResponse { get; private set; }
    public DateTime? PaidAt { get; private set; }
    public Money RefundedAmount { get; private set; } = default!;
    public DateTime? RefundedAt { get; private set; }

    public IReadOnlyCollection<Dispute> Disputes => _disputes.AsReadOnly();
}
