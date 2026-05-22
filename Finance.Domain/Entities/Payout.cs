using Finance.Domain.Enums;
using Finance.Domain.Events;
using YallaJo.SharedKernel.Domain.Entities;
using YallaJo.SharedKernel.Domain.ValueObjects;

namespace Finance.Domain.Entities;

/// <summary>
/// Aggregate root representing a periodic disbursement batch to a single provider
/// in a single currency (F-R7). One Payout per (ProviderId, Currency) per batch period.
/// </summary>
public sealed class Payout : AuditableEntity, IAggregateRoot
{
    private readonly List<PayoutItem> _payoutItems = [];

    private Payout() { } // EF Core

    // ── Grouping key ──
    public Guid ProviderId { get; private set; }
    public string Currency { get; private set; } = string.Empty;

    // ── Period ──
    public DateOnly BatchPeriodStart { get; private set; }
    public DateOnly BatchPeriodEnd { get; private set; }

    // ── Totals (computed via AddItem) ──
    public Money GrossAmount { get; private set; } = default!;
    public Money CommissionAmount { get; private set; } = default!;
    public Money NetAmount { get; private set; } = default!;

    // ── Lifecycle ──
    public PayoutStatus Status { get; private set; } = PayoutStatus.Pending;
    public DateTime? ProcessedAt { get; private set; }
    public string? FailureReason { get; private set; }

    // ── Approval ──
    public Guid? ApprovedByUserId { get; private set; }
    public DateTime? ApprovedAt { get; private set; }

    // ── Gateway ──
    public string? GatewayPayoutId { get; private set; }
    public DateTime? CompletedAt { get; private set; }

    // ── Recipient bank ──
    public Guid? BankAccountId { get; private set; }

    // ── Legacy compatibility fields (preserved until migration) ──
    public Guid RecipientUserId { get; private set; } // legacy mirror of ProviderId
    public Money TotalAmount { get; private set; } = default!;   // legacy mirror of NetAmount
    public string? BankAccountInfo { get; private set; }
    public string? TransactionId { get; private set; }            // legacy mirror of GatewayPayoutId
    public string? Notes { get; private set; }

    public IReadOnlyCollection<PayoutItem> PayoutItems => _payoutItems.AsReadOnly();

    /// <summary>
    /// Factory: creates a new Payout batch for one provider in one currency for a given period.
    /// </summary>
    public static Payout CreateBatch(
        Guid providerId,
        string currency,
        DateOnly batchPeriodStart,
        DateOnly batchPeriodEnd,
        Guid? bankAccountId,
        TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(timeProvider);
        if (providerId == Guid.Empty)
        {
            throw new ArgumentException("ProviderId is required.", nameof(providerId));
        }

        if (string.IsNullOrWhiteSpace(currency) || currency.Length != 3)
        {
            throw new ArgumentException("Currency must be a 3-letter ISO code.", nameof(currency));
        }

        if (batchPeriodEnd < batchPeriodStart)
        {
            throw new ArgumentException("BatchPeriodEnd must be >= BatchPeriodStart.", nameof(batchPeriodEnd));
        }

        var payout = new Payout
        {
            ProviderId = providerId,
            RecipientUserId = providerId, // legacy mirror
            Currency = currency.ToUpperInvariant(),
            BatchPeriodStart = batchPeriodStart,
            BatchPeriodEnd = batchPeriodEnd,
            BankAccountId = bankAccountId,
            Status = PayoutStatus.Pending,
            GrossAmount = Money.Zero(currency),
            CommissionAmount = Money.Zero(currency),
            NetAmount = Money.Zero(currency),
            TotalAmount = Money.Zero(currency),
        };
        return payout;
    }

    /// <summary>
    /// Adds one accounting line (one booking) into this payout. Updates running totals.
    /// </summary>
    public PayoutItem AddItem(
        Guid bookingId,
        Money grossAmount,
        Money commissionAmount,
        Guid? commissionRuleSnapshotId,
        TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(grossAmount);
        ArgumentNullException.ThrowIfNull(commissionAmount);
        ArgumentNullException.ThrowIfNull(timeProvider);
        if (!string.Equals(grossAmount.Currency, Currency, StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(commissionAmount.Currency, Currency, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Item currency must match payout currency.");
        }

        if (Status != PayoutStatus.Pending && Status != PayoutStatus.Hold)
        {
            throw new InvalidOperationException($"Cannot add items to payout in {Status} status.");
        }

        var netAmount = new Money(grossAmount.Amount - commissionAmount.Amount, Currency);
        var item = new PayoutItem(Id, bookingId, grossAmount, commissionAmount, netAmount, commissionRuleSnapshotId);
        _payoutItems.Add(item);

        GrossAmount = new Money(GrossAmount.Amount + grossAmount.Amount, Currency);
        CommissionAmount = new Money(CommissionAmount.Amount + commissionAmount.Amount, Currency);
        NetAmount = new Money(NetAmount.Amount + netAmount.Amount, Currency);
        TotalAmount = NetAmount; // legacy mirror

        AddDomainEvent(new PayoutItemAddedDomainEvent(Id, bookingId, grossAmount.Amount, commissionAmount.Amount, netAmount.Amount, Currency));
        MarkUpdated();
        return item;
    }

    /// <summary>Marks payout as held (no verified bank account, open dispute, etc.).</summary>
    public void PutOnHold(string reason)
    {
        if (Status == PayoutStatus.Hold)
        {
            return;
        }

        if (Status != PayoutStatus.Pending)
        {
            throw new InvalidOperationException($"Cannot hold payout in {Status} status.");
        }

        Status = PayoutStatus.Hold;
        FailureReason = reason;
        MarkUpdated();
    }

    /// <summary>Auto-approves payout below large threshold (no admin approval needed).</summary>
    public void MarkReadyForPayout()
    {
        if (Status == PayoutStatus.ReadyForPayout)
        {
            return;
        }

        if (Status != PayoutStatus.Pending)
        {
            throw new InvalidOperationException($"Cannot mark ready in {Status} status.");
        }

        Status = PayoutStatus.ReadyForPayout;
        MarkUpdated();
    }

    /// <summary>Admin approves a large payout (above large threshold).</summary>
    public void Approve(Guid approverUserId, TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(timeProvider);
        if (approverUserId == Guid.Empty)
        {
            throw new ArgumentException("ApproverUserId is required.", nameof(approverUserId));
        }

        if (Status != PayoutStatus.Pending)
        {
            throw new InvalidOperationException($"Cannot approve payout in {Status} status.");
        }

        ApprovedByUserId = approverUserId;
        ApprovedAt = timeProvider.GetUtcNow().UtcDateTime;
        Status = PayoutStatus.ReadyForPayout;
        AddDomainEvent(new PayoutApprovedDomainEvent(Id, approverUserId, ApprovedAt.Value, NetAmount.Amount, Currency, ProviderId));
        MarkUpdated();
    }

    /// <summary>Marks payout completed after gateway PayoutAsync succeeded.</summary>
    public void MarkCompleted(string gatewayPayoutId, DateTime completedAtUtc)
    {
        if (string.IsNullOrWhiteSpace(gatewayPayoutId))
        {
            throw new ArgumentException("GatewayPayoutId is required.", nameof(gatewayPayoutId));
        }

        if (Status == PayoutStatus.Completed)
        {
            return; // idempotent
        }

        if (Status != PayoutStatus.ReadyForPayout)
        {
            throw new InvalidOperationException($"Cannot complete payout in {Status} status.");
        }

        Status = PayoutStatus.Completed;
        GatewayPayoutId = gatewayPayoutId;
        TransactionId = gatewayPayoutId; // legacy mirror
        CompletedAt = completedAtUtc;
        ProcessedAt = completedAtUtc;
        AddDomainEvent(new PayoutCompletedDomainEvent(Id, ProviderId, NetAmount.Amount, Currency, gatewayPayoutId, completedAtUtc));
        MarkUpdated();
    }

    /// <summary>Marks payout failed when gateway disbursement errored.</summary>
    public void MarkFailed(string failureReason, DateTime occurredAtUtc)
    {
        if (Status == PayoutStatus.Failed)
        {
            return; // idempotent
        }

        Status = PayoutStatus.Failed;
        FailureReason = failureReason;
        AddDomainEvent(new PayoutFailedDomainEvent(Id, ProviderId, failureReason, occurredAtUtc));
        MarkUpdated();
    }

    /// <summary>
    /// Domain event raised once after a batching run when the payout is first persisted.
    /// </summary>
    public void RaiseScheduled(int itemCount)
    {
        AddDomainEvent(new PayoutBatchCreatedDomainEvent(Id, BatchPeriodStart, BatchPeriodEnd, NetAmount.Amount, Currency, itemCount));
    }
}
