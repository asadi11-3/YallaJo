using Finance.Domain.Enums;
using Finance.Domain.Events;
using YallaJo.SharedKernel.Domain.Entities;
using YallaJo.SharedKernel.Domain.ValueObjects;

namespace Finance.Domain.Entities;

/// <summary>
/// Payment aggregate root.
/// Represents both forward charges (Type=Booking) and reversals (Type=Refund, with
/// negative amount and OriginalPaymentId backreference). All money flows through
/// platform escrow before being released to providers via Payout.
/// </summary>
public sealed class Payment : AuditableEntity, IAggregateRoot
{
    private readonly List<Dispute> _disputes = [];

    private Payment() { } // EF Core

    // ── Ownership ──
    public Guid UserId { get; private set; }
    public Guid? BookingId { get; private set; }
    public Guid? ReservationId { get; private set; }
    public Guid ProviderId { get; private set; }

    // ── Amount + currency (F-R1) ──
    public Money Amount { get; private set; } = default!;
    public string Currency { get; private set; } = string.Empty;

    // ── Payment classification ──
    public PaymentMethod PaymentMethod { get; private set; }
    public PaymentType PaymentType { get; private set; } = PaymentType.Booking;
    public PaymentStatus Status { get; private set; } = PaymentStatus.Pending;
    public Guid? OriginalPaymentId { get; private set; }

    // ── Gateway integration ──
    public string GatewayProvider { get; private set; } = string.Empty;
    public string? GatewayTransactionId { get; private set; }
    public string? GatewayResponse { get; private set; }
    public Uri? RedirectUrl { get; private set; }
    public string? ClientSecret { get; private set; }

    // ── Escrow + lifecycle (F-R4) ──
    public string RecipientAccount { get; private set; } = "platform-escrow";
    public DateTime? ExpiresAt { get; private set; }
    public DateTime? EscrowReleaseEligibleAt { get; private set; }
    public DateTime? PaidAt { get; private set; }

    // ── Refund tracking (F-R5) ──
    public Money RefundedAmount { get; private set; } = default!;
    public Money RefundedTotal { get; private set; } = default!;
    public DateTime? RefundedAt { get; private set; }

    // ── Retry tracking (T5 RefundRetryService) ──
    public int RetryCount { get; private set; }

    // ── Legacy field retained for backward compat with existing migrations ──
    public string? TransactionId { get; private set; }

    public IReadOnlyCollection<Dispute> Disputes => _disputes.AsReadOnly();

    /// <summary>
    /// Factory: initiate a forward Booking payment.
    /// Raised event: <see cref="PaymentInitiatedDomainEvent"/>.
    /// </summary>
    public static Payment Initiate(
        Guid bookingId,
        Guid userId,
        Guid providerId,
        Money amount,
        PaymentMethod paymentMethod,
        string gatewayProvider,
        TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(amount);
        ArgumentException.ThrowIfNullOrWhiteSpace(gatewayProvider);
        ArgumentNullException.ThrowIfNull(timeProvider);
        if (amount.Amount <= 0m)
        {
            throw new ArgumentException("Amount must be positive.", nameof(amount));
        }

        var payment = new Payment
        {
            UserId = userId,
            BookingId = bookingId,
            ProviderId = providerId,
            Amount = amount,
            Currency = amount.Currency,
            PaymentMethod = paymentMethod,
            PaymentType = PaymentType.Booking,
            Status = PaymentStatus.Pending,
            GatewayProvider = gatewayProvider,
            RecipientAccount = "platform-escrow",
            RefundedAmount = Money.Zero(amount.Currency),
            RefundedTotal = Money.Zero(amount.Currency),
            ExpiresAt = timeProvider.GetUtcNow().UtcDateTime.AddMinutes(15),
        };

        payment.AddDomainEvent(new PaymentInitiatedDomainEvent(
            PaymentId: payment.Id,
            BookingId: bookingId,
            UserId: userId,
            ProviderId: providerId,
            Amount: amount.Amount,
            Currency: amount.Currency));

        return payment;
    }

    /// <summary>
    /// Stamp gateway-returned identifiers on a freshly-initiated payment.
    /// Called immediately after a successful gateway InitiateAsync round-trip.
    /// </summary>
    public void StampGatewayInitiation(
        string gatewayPaymentId,
        Uri? redirectUrl,
        string? clientSecret)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(gatewayPaymentId);
        GatewayTransactionId = gatewayPaymentId;
        TransactionId = gatewayPaymentId; // keep legacy column populated for back-compat
        RedirectUrl = redirectUrl;
        ClientSecret = clientSecret;
        MarkUpdated();
    }

    /// <summary>
    /// Mark a Pending payment as expired (used by T2 webhook timeout sweeper).
    /// </summary>
    public void MarkExpired(DateTime nowUtc)
    {
        if (Status != PaymentStatus.Pending)
        {
            return;
        }

        Status = PaymentStatus.Failed;
        PaidAt = null;
        MarkUpdated();

        AddDomainEvent(new PaymentFailedDomainEvent(
            PaymentId: Id,
            BookingId: BookingId ?? Guid.Empty,
            ReasonCode: "Payment.Expired",
            RawReason: "Pending payment expired before gateway confirmation.",
            OccurredAt: nowUtc));
    }


    /// <summary>
    /// Per F-R4 (escrow model): called when the booking is marked Completed.
    /// Sets the eligibility timestamp = completedAt + holdPeriod (default 7 days),
    /// after which the PayoutBatchingService will sweep the payment into a payout.
    /// </summary>
    public void MarkEscrowEligible(DateTime completedAtUtc, TimeSpan holdPeriod)
    {
        if (Status != PaymentStatus.Completed)
        {
            // Only Completed booking-type payments enter escrow eligibility.
            return;
        }

        var eligibleAt = completedAtUtc + holdPeriod;
        if (EscrowReleaseEligibleAt.HasValue && EscrowReleaseEligibleAt.Value <= eligibleAt)
        {
            // Already eligible at an earlier time — keep the earlier one.
            return;
        }

        EscrowReleaseEligibleAt = eligibleAt;
        MarkUpdated();
    }


    /// <summary>
    /// Webhook handler: gateway confirmed the charge completed.
    /// Idempotent: a repeat call with the same gateway txn id is a no-op.
    /// </summary>
    public void MarkCompleted(string gatewayTransactionId, DateTime occurredAtUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(gatewayTransactionId);

        // F-R3 idempotency: already completed for this gateway txn id → no-op
        if (Status == PaymentStatus.Completed && GatewayTransactionId == gatewayTransactionId)
        {
            return;
        }

        if (Status != PaymentStatus.Pending && Status != PaymentStatus.Processing)
        {
            throw new InvalidOperationException(
                $"Cannot complete payment in status {Status}.");
        }

        Status = PaymentStatus.Completed;
        GatewayTransactionId = gatewayTransactionId;
        TransactionId = gatewayTransactionId;
        PaidAt = occurredAtUtc;
        MarkUpdated();

        AddDomainEvent(new PaymentCompletedDomainEvent(
            PaymentId: Id,
            BookingId: BookingId ?? Guid.Empty,
            UserId: UserId,
            ProviderId: ProviderId,
            Amount: Amount.Amount,
            Currency: Currency,
            GatewayTransactionId: gatewayTransactionId));
    }

    /// <summary>
    /// Webhook handler: gateway reported the charge failed.
    /// Idempotent: a repeat call when already Failed is a no-op.
    /// </summary>
    public void MarkFailed(string reasonCode, string rawReason, DateTime occurredAtUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reasonCode);

        if (Status == PaymentStatus.Failed)
        {
            return;
        }

        if (Status == PaymentStatus.Completed)
        {
            throw new InvalidOperationException(
                "Cannot fail a payment that is already Completed.");
        }

        Status = PaymentStatus.Failed;
        GatewayResponse = rawReason;
        MarkUpdated();

        AddDomainEvent(new PaymentFailedDomainEvent(
            PaymentId: Id,
            BookingId: BookingId ?? Guid.Empty,
            ReasonCode: reasonCode,
            RawReason: rawReason ?? string.Empty,
            OccurredAt: occurredAtUtc));
    }

    /// <summary>
    /// Factory: create a Refund-type sibling Payment that points at this original via OriginalPaymentId.
    /// The refund row carries a negative amount (debit reversal of the platform escrow).
    /// </summary>
    public Payment CreateRefund(
        Money refundAmount,
        string reason,
        string gatewayProvider,
        TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(refundAmount);
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);
        ArgumentException.ThrowIfNullOrWhiteSpace(gatewayProvider);
        ArgumentNullException.ThrowIfNull(timeProvider);

        if (Status != PaymentStatus.Completed
            && Status != PaymentStatus.Refunded
            && Status != PaymentStatus.PartiallyRefunded)
        {
            throw new InvalidOperationException(
                $"Cannot refund a payment in status {Status}.");
        }

        if (!string.Equals(refundAmount.Currency, Currency, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("Refund currency must match original payment currency.", nameof(refundAmount));
        }

        if (refundAmount.Amount <= 0m)
        {
            throw new ArgumentException("Refund amount must be positive.", nameof(refundAmount));
        }

        var remaining = Amount.Amount - RefundedTotal.Amount;
        if (refundAmount.Amount > remaining)
        {
            throw new InvalidOperationException(
                "Refund amount exceeds refundable balance.");
        }

        var negativeAmount = new Money(-refundAmount.Amount, refundAmount.Currency);

        var refund = new Payment
        {
            UserId = UserId,
            BookingId = BookingId,
            ProviderId = ProviderId,
            Amount = negativeAmount,
            Currency = Currency,
            PaymentMethod = PaymentMethod,
            PaymentType = PaymentType.Refund,
            Status = PaymentStatus.Pending,
            OriginalPaymentId = Id,
            GatewayProvider = gatewayProvider,
            RecipientAccount = "platform-escrow",
            GatewayResponse = reason,
            RefundedAmount = Money.Zero(Currency),
            RefundedTotal = Money.Zero(Currency),
        };

        refund.AddDomainEvent(new RefundInitiatedDomainEvent(
            RefundPaymentId: refund.Id,
            OriginalPaymentId: Id,
            BookingId: BookingId ?? Guid.Empty,
            Amount: refundAmount.Amount,
            Currency: Currency,
            Reason: reason));

        return refund;
    }

    /// <summary>
    /// Stamp gateway-returned refund identifier on a freshly-initiated refund row.
    /// Called after a successful gateway RefundAsync round-trip.
    /// </summary>
    public void StampGatewayRefund(string gatewayRefundId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(gatewayRefundId);

        if (PaymentType != PaymentType.Refund)
        {
            throw new InvalidOperationException(
                "StampGatewayRefund only applies to Refund-type payments.");
        }

        GatewayTransactionId = gatewayRefundId;
        TransactionId = gatewayRefundId;
        MarkUpdated();
    }

    /// <summary>
    /// Mark a refund row as completed; gateway reported success.
    /// Idempotent.
    /// </summary>
    public void MarkRefundCompleted(string gatewayRefundId, DateTime occurredAtUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(gatewayRefundId);

        if (PaymentType != PaymentType.Refund)
        {
            throw new InvalidOperationException(
                "MarkRefundCompleted only applies to Refund-type payments.");
        }

        if (Status == PaymentStatus.Completed)
        {
            return;
        }

        Status = PaymentStatus.Completed;
        GatewayTransactionId = gatewayRefundId;
        TransactionId = gatewayRefundId;
        PaidAt = occurredAtUtc;
        MarkUpdated();

        AddDomainEvent(new RefundCompletedDomainEvent(
            RefundPaymentId: Id,
            OriginalPaymentId: OriginalPaymentId ?? Guid.Empty,
            BookingId: BookingId ?? Guid.Empty,
            Amount: Math.Abs(Amount.Amount),
            Currency: Currency,
            Reason: GatewayResponse ?? string.Empty,
            GatewayRefundId: gatewayRefundId));
    }

    /// <summary>
    /// Mark a refund row as failed; bumps RetryCount for T5 RefundRetryService.
    /// </summary>
    public void MarkRefundFailed(string failureReason, DateTime occurredAtUtc)
    {
        if (PaymentType != PaymentType.Refund)
        {
            throw new InvalidOperationException(
                "MarkRefundFailed only applies to Refund-type payments.");
        }

        Status = PaymentStatus.Failed;
        GatewayResponse = failureReason;
        RetryCount += 1;
        MarkUpdated();

        AddDomainEvent(new RefundFailedDomainEvent(
            RefundPaymentId: Id,
            OriginalPaymentId: OriginalPaymentId ?? Guid.Empty,
            BookingId: BookingId ?? Guid.Empty,
            Amount: Math.Abs(Amount.Amount),
            Currency: Currency,
            Reason: GatewayResponse ?? string.Empty,
            AttemptCount: RetryCount,
            FailureReason: failureReason ?? string.Empty));
    }

    /// <summary>
    /// Called on the ORIGINAL payment when a child refund completes.
    /// Accumulates RefundedTotal and transitions Status to PartiallyRefunded / Refunded.
    /// </summary>
    public void ApplyRefundCompletion(Money refundedAmount, DateTime occurredAtUtc)
    {
        ArgumentNullException.ThrowIfNull(refundedAmount);

        if (PaymentType != PaymentType.Booking)
        {
            throw new InvalidOperationException(
                "ApplyRefundCompletion only applies to Booking-type payments.");
        }

        if (!string.Equals(refundedAmount.Currency, Currency, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("Refunded currency must match payment currency.", nameof(refundedAmount));
        }

        RefundedTotal = new Money(RefundedTotal.Amount + refundedAmount.Amount, Currency);
        RefundedAmount = RefundedTotal;
        RefundedAt = occurredAtUtc;

        Status = RefundedTotal.Amount >= Amount.Amount
            ? PaymentStatus.Refunded
            : PaymentStatus.PartiallyRefunded;

        MarkUpdated();
    }

    /// <summary>
    /// Called by T5 RefundRetryService when a refund has reached MaxRetries.
    /// Raises a terminal integration event for ops alerting.
    /// </summary>
    public void IncrementRetryCount()
    {
        RetryCount += 1;
        MarkUpdated();
    }

    /// <summary>
    /// Called by T5 RefundRetryService when a refund has exceeded MaxRetries.
    /// </summary>
    public void RaiseRefundFinallyFailed(string failureReason, DateTime occurredAtUtc)
    {
        if (PaymentType != PaymentType.Refund)
        {
            throw new InvalidOperationException(
                "RaiseRefundFinallyFailed only applies to Refund-type payments.");
        }

        AddDomainEvent(new RefundFailedDomainEvent(
            RefundPaymentId: Id,
            OriginalPaymentId: OriginalPaymentId ?? Guid.Empty,
            BookingId: BookingId ?? Guid.Empty,
            Amount: Math.Abs(Amount.Amount),
            Currency: Currency,
            Reason: GatewayResponse ?? string.Empty,
            AttemptCount: RetryCount,
            FailureReason: failureReason ?? string.Empty));
    }
}
