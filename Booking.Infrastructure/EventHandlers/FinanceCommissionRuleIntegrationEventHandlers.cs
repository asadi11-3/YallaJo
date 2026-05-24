using Booking.Application.Interfaces;
using Booking.Domain.Entities;
using Booking.Domain.Repositories;
using Finance.Contracts.IntegrationEvents;
using MediatR;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Booking.Infrastructure.EventHandlers;

public sealed class FinanceCommissionRuleUpsertedIntegrationEventHandler(
    ICommissionSnapshotRepository snapshotRepository,
    IBookingInboxStore inboxStore,
    IBookingUnitOfWork unitOfWork,
    ILogger<FinanceCommissionRuleUpsertedIntegrationEventHandler> logger)
    : INotificationHandler<IntegrationEventNotification<CommissionRuleUpsertedIntegrationEvent>>
{
    public async Task Handle(
        IntegrationEventNotification<CommissionRuleUpsertedIntegrationEvent> notification,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(notification);

        if (await inboxStore.HasBeenProcessedAsync(notification.MessageId, cancellationToken).ConfigureAwait(false))
        {
            return;
        }

        var evt = notification.Event;
        var existing = await snapshotRepository
            .GetByIdAsync(evt.RuleId, cancellationToken, asNoTracking: false)
            .ConfigureAwait(false);

        if (existing is null)
        {
            var snapshot = CommissionSnapshot.Create(
                ruleId: evt.RuleId,
                tier: evt.Tier,
                currency: evt.Currency,
                percentage: evt.Percentage,
                occurredAt: evt.OccurredAt);
            await snapshotRepository.AddAsync(snapshot, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            existing.ApplyUpsert(evt.Tier, evt.Currency, evt.Percentage, evt.OccurredAt);
            snapshotRepository.Update(existing);
        }

        inboxStore.MarkAsProcessed(notification.MessageId);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        logger.LogInformation(
            "Booking commission snapshot upserted for RuleId={RuleId} Tier={Tier} Currency={Currency} Percentage={Percentage}",
            evt.RuleId, evt.Tier, evt.Currency, evt.Percentage);
    }
}

public sealed class FinanceCommissionRuleDeletedIntegrationEventHandler(
    ICommissionSnapshotRepository snapshotRepository,
    IBookingInboxStore inboxStore,
    IBookingUnitOfWork unitOfWork,
    ILogger<FinanceCommissionRuleDeletedIntegrationEventHandler> logger)
    : INotificationHandler<IntegrationEventNotification<CommissionRuleDeletedIntegrationEvent>>
{
    public async Task Handle(
        IntegrationEventNotification<CommissionRuleDeletedIntegrationEvent> notification,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(notification);

        if (await inboxStore.HasBeenProcessedAsync(notification.MessageId, cancellationToken).ConfigureAwait(false))
        {
            return;
        }

        var evt = notification.Event;
        var existing = await snapshotRepository
            .GetByIdAsync(evt.RuleId, cancellationToken, asNoTracking: false)
            .ConfigureAwait(false);

        if (existing is not null)
        {
            existing.Deactivate(evt.OccurredAt);
            snapshotRepository.Update(existing);
        }
        else
        {
            logger.LogDebug(
                "Received CommissionRuleDeleted for unknown RuleId={RuleId}; recording inbox-only.",
                evt.RuleId);
        }

        inboxStore.MarkAsProcessed(notification.MessageId);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        logger.LogInformation(
            "Booking commission snapshot deactivated for RuleId={RuleId}",
            evt.RuleId);
    }
}
