using Booking.Contracts.IntegrationEvents;
using MediatR;
using Messaging.Application.Interfaces;
using Messaging.Domain.Entities;
using Messaging.Domain.Enums;
using Messaging.Infrastructure.Persistence;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Messaging.Infrastructure.EventHandlers;

public sealed class BookingCancelledHandler(
    MessagingDbContext dbContext,
    IMessagingUnitOfWork unitOfWork,
    IMessagingInboxStore inboxStore,
    ILogger<BookingCancelledHandler> logger)
    : INotificationHandler<IntegrationEventNotification<TourBookingCancelledIntegrationEvent>>
{
    public async Task Handle(IntegrationEventNotification<TourBookingCancelledIntegrationEvent> notification, CancellationToken ct)
    {
        if (await inboxStore.HasBeenProcessedAsync(notification.MessageId, ct)) return;
        var evt = notification.Event;

        dbContext.Notifications.Add(Notification.Create(evt.UserId, NotificationType.BookingCancelled, NotificationChannel.InApp, "Booking Cancelled", "Your booking has been cancelled.", NotificationPriority.High, entityType: "Booking", entityId: evt.BookingId));

        inboxStore.MarkAsProcessed(notification.MessageId);
        await unitOfWork.SaveChangesAsync(ct);
        logger.LogInformation("Queued booking cancelled notification for booking {BookingId}", evt.BookingId);
    }
}
