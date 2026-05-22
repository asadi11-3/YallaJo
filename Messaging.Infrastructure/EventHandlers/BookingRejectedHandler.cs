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
/// Consumes <see cref="TourBookingRejectedIntegrationEvent"/> and creates an InApp
/// notification for the customer informing them that the provider declined their
/// booking and any refund amount that will be processed.
/// </summary>
public sealed class BookingRejectedHandler(
    MessagingDbContext dbContext,
    IMessagingUnitOfWork unitOfWork,
    IMessagingInboxStore inboxStore,
    ILogger<BookingRejectedHandler> logger)
    : INotificationHandler<IntegrationEventNotification<TourBookingRejectedIntegrationEvent>>
{
    public async Task Handle(
        IntegrationEventNotification<TourBookingRejectedIntegrationEvent> notification,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(notification);

        if (await inboxStore.HasBeenProcessedAsync(notification.MessageId, ct)) return;
        var evt = notification.Event;

        var body = evt.RefundAmount > 0m
            ? $"Your booking was declined by the provider. A refund of {evt.RefundAmount} {evt.Currency} will be processed."
            : "Your booking was declined by the provider.";
        if (!string.IsNullOrWhiteSpace(evt.Reason))
        {
            body += $" Reason: {evt.Reason}";
        }

        dbContext.Notifications.Add(Notification.Create(
            evt.UserId,
            NotificationType.BookingCancelled,
            NotificationChannel.InApp,
            "Booking Declined",
            body,
            NotificationPriority.High,
            entityType: "TourBooking",
            entityId: evt.BookingId));

        inboxStore.MarkAsProcessed(notification.MessageId);
        await unitOfWork.SaveChangesAsync(ct);
        logger.LogInformation(
            "Queued booking rejected notification for BookingId={BookingId} UserId={UserId}",
            evt.BookingId, evt.UserId);
    }
}
