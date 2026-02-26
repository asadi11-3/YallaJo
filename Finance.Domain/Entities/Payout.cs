using Finance.Domain.Enums;
using YallaJo.SharedKernel.Domain.Entities;

namespace Finance.Domain.Entities;

public sealed class Payout : AuditableEntity, IAggregateRoot
{
    private readonly List<PayoutItem> _payoutItems = [];

    private Payout() { } // EF Core

    public Guid RecipientUserId { get; private set; }
    public PayoutStatus Status { get; private set; } = PayoutStatus.Pending;
    public decimal TotalAmount { get; private set; }
    public string Currency { get; private set; } = string.Empty;
    public DateTime? ProcessedAt { get; private set; }
    public string? BankAccountInfo { get; private set; }
    public string? TransactionId { get; private set; }
    public string? Notes { get; private set; }

    public IReadOnlyCollection<PayoutItem> PayoutItems => _payoutItems.AsReadOnly();
}
