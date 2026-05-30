using Booking.Contracts.IntegrationEvents;
using MediatR;
using Messaging.Application.Interfaces;
using Messaging.Domain.Entities;
using Messaging.Domain.Enums;
using Messaging.Infrastructure.Persistence;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Messaging.Infrastructure.EventHandlers;

public sealed class BookingCompletedHandler(
    MessagingDbContext dbContext,
    IMessagingUnitOfWork unitOfWork,
    IMessagingInboxStore inboxStore,
    ILogger<BookingCompletedHandler> logger)
    : INotificationHandler<IntegrationEventNotification<TourBookingCompletedIntegrationEvent>>
{
    public async Task Handle(IntegrationEventNotification<TourBookingCompletedIntegrationEvent> notification, CancellationToken ct)
    {
        if (await inboxStore.HasBeenProcessedAsync(notification.MessageId, ct)) return;
        var evt = notification.Event;

        dbContext.Notifications.Add(Notification.Create(evt.UserId, NotificationType.BookingCompleted, NotificationChannel.InApp, "Trip Completed!", "Hope you had a great time! Please leave a review.", NotificationPriority.Medium, entityType: "Booking", entityId: evt.BookingId));

        inboxStore.MarkAsProcessed(notification.MessageId);
        await unitOfWork.SaveChangesAsync(ct);
        logger.LogInformation("Queued booking completed notification for booking {BookingId}", evt.BookingId);
    }
}
