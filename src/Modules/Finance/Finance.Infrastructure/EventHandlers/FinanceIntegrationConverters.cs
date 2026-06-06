using Finance.Contracts.IntegrationEvents;
using Finance.Domain.Entities;
using Finance.Domain.Events;
using Finance.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Finance.Infrastructure.EventHandlers;

/// <summary>
/// Converts <see cref="PaymentCompletedDomainEvent"/> into the outbound integration event.
/// Dispatched BEFORE SaveChanges inside the same UoW so the OutboxMessage row is persisted atomically.
/// </summary>
internal sealed class PublishPaymentCompletedHandler(
    IFinanceOutboxWriter outbox,
    ILogger<PublishPaymentCompletedHandler> logger)
    : INotificationHandler<DomainEventNotification<PaymentCompletedDomainEvent>>
{
    public async Task Handle(DomainEventNotification<PaymentCompletedDomainEvent> notification, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(notification);
        var e = notification.Event;
        var integration = new PaymentCompletedIntegrationEvent(
            PaymentId: e.PaymentId,
            BookingId: e.BookingId,
            UserId: e.UserId,
            ProviderId: e.ProviderId,
            Amount: e.Amount,
            Currency: e.Currency,
            GatewayTransactionId: e.GatewayTransactionId,
            CompletedAt: e.OccurredOn);
        await outbox.WriteAsync(integration, ct).ConfigureAwait(false);
        logger.LogInformation("Enqueued finance.payment.completed.v1 for Payment {PaymentId}", e.PaymentId);
    }
}

/// <summary>Converts <see cref="PaymentFailedDomainEvent"/> into the outbound integration event.</summary>
internal sealed class PublishPaymentFailedHandler(
    IFinanceOutboxWriter outbox,
    ILogger<PublishPaymentFailedHandler> logger)
    : INotificationHandler<DomainEventNotification<PaymentFailedDomainEvent>>
{
    public async Task Handle(DomainEventNotification<PaymentFailedDomainEvent> notification, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(notification);
        var e = notification.Event;
        var integration = new PaymentFailedIntegrationEvent(
            PaymentId: e.PaymentId,
            BookingId: e.BookingId,
            ReasonCode: e.ReasonCode,
            RawReason: e.RawReason,
            OccurredAt: e.OccurredAt);
        await outbox.WriteAsync(integration, ct).ConfigureAwait(false);
        logger.LogInformation("Enqueued finance.payment.failed.v1 for Payment {PaymentId} ({ReasonCode})", e.PaymentId, e.ReasonCode);
    }
}

/// <summary>Converts <see cref="RefundInitiatedDomainEvent"/> into the outbound integration event.</summary>
internal sealed class PublishRefundInitiatedHandler(
    IFinanceOutboxWriter outbox,
    ILogger<PublishRefundInitiatedHandler> logger)
    : INotificationHandler<DomainEventNotification<RefundInitiatedDomainEvent>>
{
    public async Task Handle(DomainEventNotification<RefundInitiatedDomainEvent> notification, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(notification);
        var e = notification.Event;
        var integration = new RefundInitiatedIntegrationEvent(
            RefundPaymentId: e.RefundPaymentId,
            OriginalPaymentId: e.OriginalPaymentId,
            BookingId: e.BookingId,
            Amount: e.Amount,
            Currency: e.Currency,
            Reason: e.Reason,
            InitiatedAt: e.OccurredOn);
        await outbox.WriteAsync(integration, ct).ConfigureAwait(false);
        logger.LogInformation("Enqueued finance.refund.initiated.v1 for RefundId {RefundId}", e.RefundPaymentId);
    }
}

/// <summary>
/// Converts <see cref="RefundCompletedDomainEvent"/> into the outbound integration event.
/// Looks up the OriginalPayment aggregate to enrich UserId for downstream consumers
/// (Messaging needs UserId to deliver a refund-completed notification to the traveler).
/// </summary>
internal sealed class PublishRefundCompletedHandler(
    IFinanceOutboxWriter outbox,
    IPaymentRepository paymentRepository,
    ILogger<PublishRefundCompletedHandler> logger)
    : INotificationHandler<DomainEventNotification<RefundCompletedDomainEvent>>
{
    public async Task Handle(DomainEventNotification<RefundCompletedDomainEvent> notification, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(notification);
        var e = notification.Event;

        // Domain event fires during SaveChanges; the original captured payment is in the change tracker
        // and carries the UserId of the traveler who paid (refund Payment row also has UserId, but
        // looking up the original captured payment is the canonical reference).
        Payment? originalPayment = await paymentRepository.GetByIdAsync(e.OriginalPaymentId, ct).ConfigureAwait(false);
        if (originalPayment is null)
        {
            logger.LogWarning(
                "Finance: OriginalPayment {OriginalPaymentId} not found during RefundCompleted converter — emitting event without UserId.",
                e.OriginalPaymentId);
        }

        var integration = new RefundCompletedIntegrationEvent(
            RefundPaymentId: e.RefundPaymentId,
            OriginalPaymentId: e.OriginalPaymentId,
            BookingId: e.BookingId,
            Amount: e.Amount,
            Currency: e.Currency,
            Reason: e.Reason,
            GatewayRefundId: e.GatewayRefundId,
            CompletedAt: e.OccurredOn,
            UserId: originalPayment?.UserId ?? Guid.Empty);
        await outbox.WriteAsync(integration, ct).ConfigureAwait(false);
        logger.LogInformation("Enqueued finance.refund.completed.v1 for RefundId {RefundId}", e.RefundPaymentId);
    }
}

/// <summary>Converts <see cref="RefundFailedDomainEvent"/> into the outbound integration event.</summary>
internal sealed class PublishRefundFailedHandler(
    IFinanceOutboxWriter outbox,
    ILogger<PublishRefundFailedHandler> logger)
    : INotificationHandler<DomainEventNotification<RefundFailedDomainEvent>>
{
    public async Task Handle(DomainEventNotification<RefundFailedDomainEvent> notification, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(notification);
        var e = notification.Event;
        var integration = new RefundFailedIntegrationEvent(
            RefundPaymentId: e.RefundPaymentId,
            OriginalPaymentId: e.OriginalPaymentId,
            BookingId: e.BookingId,
            Amount: e.Amount,
            Currency: e.Currency,
            Reason: e.Reason,
            AttemptCount: e.AttemptCount,
            FailureReason: e.FailureReason,
            FailedAt: e.OccurredOn);
        await outbox.WriteAsync(integration, ct).ConfigureAwait(false);
        logger.LogInformation("Enqueued finance.refund.failed.v1 for RefundId {RefundId} (attempt {Attempt})", e.RefundPaymentId, e.AttemptCount);
    }
}

/// <summary>
/// Converts <see cref="DisputeOpenedDomainEvent"/> into the outbound integration event.
/// Domain event already carries UserId, so no repository lookup is required.
/// Consumers: Messaging (notifies the disputing traveler), Analytics (dispute funnel).
/// </summary>
internal sealed class PublishDisputeOpenedHandler(
    IFinanceOutboxWriter outbox,
    ILogger<PublishDisputeOpenedHandler> logger)
    : INotificationHandler<DomainEventNotification<DisputeOpenedDomainEvent>>
{
    public async Task Handle(DomainEventNotification<DisputeOpenedDomainEvent> notification, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(notification);
        var e = notification.Event;
        var integration = new DisputeOpenedIntegrationEvent(
            DisputeId: e.DisputeId,
            PaymentId: e.PaymentId,
            UserId: e.UserId,
            Reason: e.Reason);
        await outbox.WriteAsync(integration, ct).ConfigureAwait(false);
        logger.LogInformation("Enqueued finance.dispute-opened.v1 for Dispute {DisputeId} (Payment {PaymentId})", e.DisputeId, e.PaymentId);
    }
}

/// <summary>
/// Converts <see cref="InvoiceGeneratedDomainEvent"/> into the outbound integration event.
/// Looks up the Invoice aggregate for InvoiceNumber + PaymentId + IssuedAt fields not carried on the domain event.
/// </summary>
internal sealed class PublishInvoiceGeneratedHandler(
    IFinanceOutboxWriter outbox,
    IInvoiceRepository invoiceRepository,
    ILogger<PublishInvoiceGeneratedHandler> logger)
    : INotificationHandler<DomainEventNotification<InvoiceGeneratedDomainEvent>>
{
    public async Task Handle(DomainEventNotification<InvoiceGeneratedDomainEvent> notification, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(notification);
        var e = notification.Event;

        // Domain event fires during SaveChanges; Invoice is in the change tracker.
        Invoice? invoice = await invoiceRepository.GetByIdAsync(e.InvoiceId, ct).ConfigureAwait(false);
        if (invoice is null)
        {
            logger.LogWarning(
                "Finance: Invoice {InvoiceId} not found during InvoiceGenerated converter — skipping integration event emission.",
                e.InvoiceId);
            return;
        }

        var integration = new InvoiceGeneratedIntegrationEvent(
            InvoiceId: e.InvoiceId,
            PaymentId: invoice.PaymentId,
            BookingId: e.BookingId,
            UserId: e.UserId,
            ProviderId: e.ProviderId,
            InvoiceNumber: invoice.InvoiceNumber,
            AmountTotal: e.AmountTotal,
            AmountSubtotal: e.AmountSubtotal,
            AmountTax: e.AmountTax,
            AmountDiscount: e.AmountDiscount,
            Currency: e.Currency,
            IssuedAt: invoice.IssuedAt);
        await outbox.WriteAsync(integration, ct).ConfigureAwait(false);
        logger.LogInformation("Enqueued finance.invoice.generated.v1 for Invoice {InvoiceId} ({InvoiceNumber})", e.InvoiceId, invoice.InvoiceNumber);
    }
}

/// <summary>
/// Converts <see cref="PayoutBatchCreatedDomainEvent"/> into the outbound integration event
/// (logical name <c>finance.payout.scheduled.v1</c>). Looks up the Payout aggregate to read
/// ProviderId / GrossAmount / CommissionAmount / NetAmount / Status fields not carried on the domain event.
/// </summary>
internal sealed class PublishPayoutScheduledHandler(
    IFinanceOutboxWriter outbox,
    IPayoutRepository payoutRepository,
    ILogger<PublishPayoutScheduledHandler> logger)
    : INotificationHandler<DomainEventNotification<PayoutBatchCreatedDomainEvent>>
{
    public async Task Handle(DomainEventNotification<PayoutBatchCreatedDomainEvent> notification, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(notification);
        var e = notification.Event;

        var payout = await payoutRepository.GetByIdAsync(e.PayoutBatchId, ct).ConfigureAwait(false);
        if (payout is null)
        {
            logger.LogWarning(
                "Finance: Payout {PayoutId} not found during PayoutBatchCreated converter — skipping integration event emission.",
                e.PayoutBatchId);
            return;
        }

        var integration = new PayoutScheduledIntegrationEvent(
            PayoutId: e.PayoutBatchId,
            ProviderId: payout.ProviderId,
            BatchPeriodStart: e.BatchPeriodStart,
            BatchPeriodEnd: e.BatchPeriodEnd,
            GrossAmount: payout.GrossAmount.Amount,
            CommissionAmount: payout.CommissionAmount.Amount,
            NetAmount: payout.NetAmount.Amount,
            Currency: e.Currency,
            ItemCount: e.ItemCount,
            Status: payout.Status.ToString(),
            ScheduledAt: e.OccurredOn);
        await outbox.WriteAsync(integration, ct).ConfigureAwait(false);
        logger.LogInformation(
            "Enqueued finance.payout.scheduled.v1 for Payout {PayoutId} ({Items} items, status={Status})",
            e.PayoutBatchId, e.ItemCount, payout.Status);
    }
}

/// <summary>Converts <see cref="PayoutCompletedDomainEvent"/> into the outbound integration event.</summary>
internal sealed class PublishPayoutCompletedHandler(
    IFinanceOutboxWriter outbox,
    ILogger<PublishPayoutCompletedHandler> logger)
    : INotificationHandler<DomainEventNotification<PayoutCompletedDomainEvent>>
{
    public async Task Handle(DomainEventNotification<PayoutCompletedDomainEvent> notification, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(notification);
        var e = notification.Event;
        var integration = new PayoutCompletedIntegrationEvent(
            PayoutId: e.PayoutId,
            ProviderId: e.ProviderId,
            NetAmount: e.NetAmount,
            Currency: e.Currency,
            GatewayPayoutId: e.GatewayPayoutId,
            CompletedAt: e.CompletedAt);
        await outbox.WriteAsync(integration, ct).ConfigureAwait(false);
        logger.LogInformation("Enqueued finance.payout.completed.v1 for Payout {PayoutId}", e.PayoutId);
    }
}

/// <summary>Converts <see cref="CommissionRuleUpsertedDomainEvent"/> into the outbound integration event.</summary>
internal sealed class PublishCommissionRuleUpsertedHandler(
    IFinanceOutboxWriter outbox,
    ILogger<PublishCommissionRuleUpsertedHandler> logger)
    : INotificationHandler<DomainEventNotification<CommissionRuleUpsertedDomainEvent>>
{
    public async Task Handle(DomainEventNotification<CommissionRuleUpsertedDomainEvent> notification, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(notification);
        var e = notification.Event;
        var integration = new CommissionRuleUpsertedIntegrationEvent(
            RuleId: e.RuleId,
            Tier: e.Tier,
            MinMonthlyRevenue: e.MinMonthlyRevenue,
            MaxMonthlyRevenue: e.MaxMonthlyRevenue,
            Currency: e.Currency,
            Percentage: e.Percentage,
            OccurredAt: e.OccurredOn);
        await outbox.WriteAsync(integration, ct).ConfigureAwait(false);
        logger.LogInformation("Enqueued finance.commission-rule.upserted.v1 for Rule {RuleId}", e.RuleId);
    }
}

/// <summary>Converts <see cref="CommissionRuleDeletedDomainEvent"/> into the outbound integration event.</summary>
internal sealed class PublishCommissionRuleDeletedHandler(
    IFinanceOutboxWriter outbox,
    ILogger<PublishCommissionRuleDeletedHandler> logger)
    : INotificationHandler<DomainEventNotification<CommissionRuleDeletedDomainEvent>>
{
    public async Task Handle(DomainEventNotification<CommissionRuleDeletedDomainEvent> notification, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(notification);
        var e = notification.Event;
        var integration = new CommissionRuleDeletedIntegrationEvent(
            RuleId: e.RuleId,
            OccurredAt: e.OccurredOn);
        await outbox.WriteAsync(integration, ct).ConfigureAwait(false);
        logger.LogInformation("Enqueued finance.commission-rule.deleted.v1 for Rule {RuleId}", e.RuleId);
    }
}

/// <summary>
/// Converts <see cref="DisputeResolvedDomainEvent"/> into the outbound integration event
/// (Phase-3 WS-4: completes the dispute lifecycle that began in Phase-2 WS-3b with DisputeOpened).
/// </summary>
internal sealed class PublishDisputeResolvedHandler(
    IFinanceOutboxWriter outbox,
    ILogger<PublishDisputeResolvedHandler> logger)
    : INotificationHandler<DomainEventNotification<DisputeResolvedDomainEvent>>
{
    public async Task Handle(DomainEventNotification<DisputeResolvedDomainEvent> notification, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(notification);
        var e = notification.Event;
        var integration = new DisputeResolvedIntegrationEvent(
            DisputeId: e.DisputeId,
            PaymentId: e.PaymentId,
            UserId: e.UserId,
            Resolution: e.Resolution.ToString(),
            ResolvedByAdminId: e.ResolvedByAdminId,
            ResolutionNotes: e.ResolutionNotes);
        await outbox.WriteAsync(integration, ct).ConfigureAwait(false);
        logger.LogInformation("Enqueued finance.dispute-resolved.v1 for Dispute {DisputeId} (resolution {Resolution})", e.DisputeId, e.Resolution);
    }
}

/// <summary>
/// Converts <see cref="DisputeEscalatedDomainEvent"/> into the outbound integration event
/// (Phase-3 WS-4: adds the missing escalation event to the dispute lifecycle).
/// </summary>
internal sealed class PublishDisputeEscalatedHandler(
    IFinanceOutboxWriter outbox,
    ILogger<PublishDisputeEscalatedHandler> logger)
    : INotificationHandler<DomainEventNotification<DisputeEscalatedDomainEvent>>
{
    public async Task Handle(DomainEventNotification<DisputeEscalatedDomainEvent> notification, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(notification);
        var e = notification.Event;
        var integration = new DisputeEscalatedIntegrationEvent(
            DisputeId: e.DisputeId,
            PaymentId: e.PaymentId,
            UserId: e.UserId,
            Reason: e.Reason,
            EscalatedByAdminId: e.EscalatedByAdminId);
        await outbox.WriteAsync(integration, ct).ConfigureAwait(false);
        logger.LogInformation("Enqueued finance.dispute-escalated.v1 for Dispute {DisputeId} (by admin {AdminId})", e.DisputeId, e.EscalatedByAdminId);
    }
}
