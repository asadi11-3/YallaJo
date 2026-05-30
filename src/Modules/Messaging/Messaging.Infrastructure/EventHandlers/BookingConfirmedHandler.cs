using Booking.Contracts.IntegrationEvents;
using MediatR;
using Messaging.Application.Interfaces;
using Messaging.Domain.Entities;
using Messaging.Domain.Enums;
using Messaging.Infrastructure.Persistence;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Messaging.Infrastructure.EventHandlers;

public sealed class BookingConfirmedHandler(
    MessagingDbContext dbContext,
    IMessagingUnitOfWork unitOfWork,
    IMessagingInboxStore inboxStore,
    ILogger<BookingConfirmedHandler> logger)
    : INotificationHandler<IntegrationEventNotification<TourBookingConfirmedIntegrationEvent>>
{
    public async Task Handle(IntegrationEventNotification<TourBookingConfirmedIntegrationEvent> notification, CancellationToken ct)
    {
        if (await inboxStore.HasBeenProcessedAsync(notification.MessageId, ct)) return;
        var evt = notification.Event;

        dbContext.Notifications.Add(Notification.Create(evt.UserId, NotificationType.BookingConfirmed, NotificationChannel.InApp, "Booking Confirmed!", "Your tour booking has been confirmed. Get ready for an amazing experience!", NotificationPriority.High, entityType: "Booking", entityId: evt.BookingId));
        dbContext.Notifications.Add(Notification.Create(evt.ProviderId, NotificationType.ProviderBookingReceived, NotificationChannel.InApp, "New Booking Received", "A tour booking has been confirmed for your service.", NotificationPriority.High, entityType: "Booking", entityId: evt.BookingId));

        inboxStore.MarkAsProcessed(notification.MessageId);
        await unitOfWork.SaveChangesAsync(ct);
        logger.LogInformation("Queued booking confirmed notifications for booking {BookingId}", evt.BookingId);
    }
}
