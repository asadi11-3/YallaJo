using Booking.Contracts.IntegrationEvents;
using MediatR;
using Messaging.Application.Interfaces;
using Messaging.Domain.Entities;
using Messaging.Domain.Enums;
using Messaging.Infrastructure.Persistence;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Messaging.Infrastructure.EventHandlers;

/// <summary>
/// Phase-3 WS-3b: Consumes <see cref="TourBookingDisputeResolvedIntegrationEvent"/> (logical name:
/// booking.tour-booking.dispute-resolved.v1) and queues notifications for both parties so each
/// learns the outcome of the dispute. Mirrors the dual-notify pattern of <c>BookingConfirmedHandler</c>.
/// Idempotency via the messaging inbox store.
/// </summary>
public sealed class TourBookingDisputeResolvedHandler(
    MessagingDbContext dbContext,
    IMessagingUnitOfWork unitOfWork,
    IMessagingInboxStore inboxStore,
    ILogger<TourBookingDisputeResolvedHandler> logger)
    : INotificationHandler<IntegrationEventNotification<TourBookingDisputeResolvedIntegrationEvent>>
{
    public async Task Handle(IntegrationEventNotification<TourBookingDisputeResolvedIntegrationEvent> notification, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(notification);

        if (await inboxStore.HasBeenProcessedAsync(notification.MessageId, ct).ConfigureAwait(false))
        {
            return;
        }

        var evt = notification.Event;

        var notesSnippet = evt.ResolutionNotes.Length > 200 ? evt.ResolutionNotes[..200] + "…" : evt.ResolutionNotes;

        // 1. Traveler — outcome of the dispute they opened.
        dbContext.Notifications.Add(Notification.Create(
            userId: evt.UserId,
            type: NotificationType.BookingDisputeResolved,
            channel: NotificationChannel.InApp,
            title: "Dispute Resolved",
            body: $"Your booking dispute has been resolved. Resolution: {notesSnippet}",
            priority: NotificationPriority.Medium,
            entityType: "Booking",
            entityId: evt.BookingId));

        // 2. Provider — closure of the dispute filed against their booking.
        dbContext.Notifications.Add(Notification.Create(
            userId: evt.ProviderId,
            type: NotificationType.BookingDisputeResolved,
            channel: NotificationChannel.InApp,
            title: "Booking Dispute Resolved",
            body: $"A dispute on one of your bookings has been resolved by support. Resolution: {notesSnippet}",
            priority: NotificationPriority.Medium,
            entityType: "Booking",
            entityId: evt.BookingId));

        inboxStore.MarkAsProcessed(notification.MessageId);
        await unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);

        logger.LogInformation(
            "Queued booking-dispute-resolved notifications for booking {BookingId} (user {UserId}, provider {ProviderId}, admin {AdminId})",
            evt.BookingId, evt.UserId, evt.ProviderId, evt.ResolvedByAdminId);
    }
}
