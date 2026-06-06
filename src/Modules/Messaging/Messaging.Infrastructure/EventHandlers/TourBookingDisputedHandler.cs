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
/// Phase-3 WS-3b: Consumes <see cref="TourBookingDisputedIntegrationEvent"/> (logical name:
/// booking.tour-booking.disputed.v1) and queues TWO in-app notifications:
///   1. Provider: high-priority alert that a traveler opened a dispute against their booking.
///   2. Traveler: acknowledgment that the dispute was registered with support.
/// Mirrors the dual-notify pattern used by <c>BookingConfirmedHandler</c>.
/// Idempotency via the messaging inbox store.
/// </summary>
public sealed class TourBookingDisputedHandler(
    MessagingDbContext dbContext,
    IMessagingUnitOfWork unitOfWork,
    IMessagingInboxStore inboxStore,
    ILogger<TourBookingDisputedHandler> logger)
    : INotificationHandler<IntegrationEventNotification<TourBookingDisputedIntegrationEvent>>
{
    public async Task Handle(IntegrationEventNotification<TourBookingDisputedIntegrationEvent> notification, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(notification);

        if (await inboxStore.HasBeenProcessedAsync(notification.MessageId, ct).ConfigureAwait(false))
        {
            return;
        }

        var evt = notification.Event;

        // Snippet of the dispute reason for the notification body (keeps the line readable).
        var reasonSnippet = evt.Reason.Length > 160 ? evt.Reason[..160] + "…" : evt.Reason;

        // 1. Provider — urgent: a dispute was filed against one of their bookings.
        dbContext.Notifications.Add(Notification.Create(
            userId: evt.ProviderId,
            type: NotificationType.BookingDisputed,
            channel: NotificationChannel.InApp,
            title: "Booking Dispute Opened",
            body: $"A traveler has opened a dispute on a completed booking. Reason: {reasonSnippet}",
            priority: NotificationPriority.High,
            entityType: "Booking",
            entityId: evt.BookingId));

        // 2. Traveler — courtesy ack: dispute was registered with support.
        dbContext.Notifications.Add(Notification.Create(
            userId: evt.UserId,
            type: NotificationType.BookingDisputed,
            channel: NotificationChannel.InApp,
            title: "Dispute Registered",
            body: "Your dispute on a completed booking has been registered. Our support team will review it shortly.",
            priority: NotificationPriority.High,
            entityType: "Booking",
            entityId: evt.BookingId));

        inboxStore.MarkAsProcessed(notification.MessageId);
        await unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);

        logger.LogInformation(
            "Queued booking-disputed notifications for booking {BookingId} (user {UserId}, provider {ProviderId})",
            evt.BookingId, evt.UserId, evt.ProviderId);
    }
}
