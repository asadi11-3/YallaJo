using Accounts.Contracts.IntegrationEvents;
using Finance.Application.Interfaces;
using Finance.Domain.Enums;
using Finance.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Finance.Infrastructure.EventHandlers;

/// <summary>
/// Puts all active payouts for a provider on hold when that provider is suspended.
///
/// <para>
/// Only affects payouts in <see cref="PayoutStatus.Pending"/> or
/// <see cref="PayoutStatus.ReadyForPayout"/> states. Completed, failed, or already-held
/// payouts are not modified.
/// </para>
/// </summary>
public sealed class ProviderSuspendedHoldPayoutsHandler(
    FinanceDbContext dbContext,
    IFinanceUnitOfWork unitOfWork,
    IFinanceInboxStore inboxStore,
    ILogger<ProviderSuspendedHoldPayoutsHandler> logger)
    : INotificationHandler<IntegrationEventNotification<ProviderSuspendedIntegrationEvent>>
{
    public async Task Handle(
        IntegrationEventNotification<ProviderSuspendedIntegrationEvent> notification,
        CancellationToken ct)
    {
        if (await inboxStore.HasBeenProcessedAsync(notification.MessageId, ct).ConfigureAwait(false))
        {
            logger.LogDebug(
                "Finance: Message {MessageId} (ProviderSuspended {ApplicationId}) already processed; skipping.",
                notification.MessageId, notification.Event.ApplicationId);
            return;
        }

        var evt = notification.Event;
        var holdReason = $"Provider account suspended: {evt.Reason}";

        var activePayouts = await dbContext.Payouts
            .Where(p => p.ProviderId == evt.UserId
                        && (p.Status == PayoutStatus.Pending || p.Status == PayoutStatus.ReadyForPayout))
            .ToListAsync(ct)
            .ConfigureAwait(false);

        if (activePayouts.Count == 0)
        {
            logger.LogInformation(
                "Finance: No active payouts to hold for ProviderSuspended UserId={UserId} ApplicationId={ApplicationId}.",
                evt.UserId, evt.ApplicationId);
        }
        else
        {
            foreach (var payout in activePayouts)
            {
                payout.PutOnHold(holdReason);
            }

            logger.LogInformation(
                "Finance: Put {Count} payout(s) on hold for ProviderSuspended UserId={UserId} ApplicationId={ApplicationId}.",
                activePayouts.Count, evt.UserId, evt.ApplicationId);
        }

        inboxStore.MarkAsProcessed(notification.MessageId);
        await unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);
    }
}
