using Booking.Contracts.IntegrationEvents;
using MediatR;
using Messaging.Application.Interfaces;
using Messaging.Domain.Entities;
using Messaging.Domain.Enums;
using Messaging.Infrastructure.Persistence;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Messaging.Infrastructure.EventHandlers;

/// <summary>Consumes <c>booking.join-request.created.v1</c> and acknowledges receipt to the requester.</summary>
public sealed class JoinRequestCreatedHandler(
    MessagingDbContext dbContext,
    IMessagingUnitOfWork unitOfWork,
    IMessagingInboxStore inboxStore,
    ILogger<JoinRequestCreatedHandler> logger)
    : INotificationHandler<IntegrationEventNotification<JoinRequestCreatedIntegrationEvent>>
{
    public async Task Handle(IntegrationEventNotification<JoinRequestCreatedIntegrationEvent> notification, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(notification);
        if (await inboxStore.HasBeenProcessedAsync(notification.MessageId, ct)) return;
        var evt = notification.Event;

        dbContext.Notifications.Add(Notification.Create(
            evt.UserId,
            NotificationType.BookingConfirmed,
            NotificationChannel.InApp,
            "Join Request Submitted",
            "Your join request was submitted. We'll notify you once the host responds.",
            NotificationPriority.Medium,
            entityType: "JoinRequest",
            entityId: evt.JoinRequestId));

        inboxStore.MarkAsProcessed(notification.MessageId);
        await unitOfWork.SaveChangesAsync(ct);
        logger.LogInformation("Queued join request submitted notification for JoinRequest {Id}", evt.JoinRequestId);
    }
}

/// <summary>Consumes <c>booking.join-request.approved.v1</c> and notifies the requester their join was accepted.</summary>
public sealed class JoinRequestApprovedHandler(
    MessagingDbContext dbContext,
    IMessagingUnitOfWork unitOfWork,
    IMessagingInboxStore inboxStore,
    ILogger<JoinRequestApprovedHandler> logger)
    : INotificationHandler<IntegrationEventNotification<JoinRequestApprovedIntegrationEvent>>
{
    public async Task Handle(IntegrationEventNotification<JoinRequestApprovedIntegrationEvent> notification, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(notification);
        if (await inboxStore.HasBeenProcessedAsync(notification.MessageId, ct)) return;
        var evt = notification.Event;

        dbContext.Notifications.Add(Notification.Create(
            evt.UserId,
            NotificationType.BookingConfirmed,
            NotificationChannel.InApp,
            "Join Request Approved",
            "Good news! The host has approved your join request.",
            NotificationPriority.High,
            entityType: "JoinRequest",
            entityId: evt.JoinRequestId));

        inboxStore.MarkAsProcessed(notification.MessageId);
        await unitOfWork.SaveChangesAsync(ct);
        logger.LogInformation("Queued join request approved notification for JoinRequest {Id}", evt.JoinRequestId);
    }
}

/// <summary>Consumes <c>booking.join-request.rejected.v1</c> and notifies the requester of the rejection.</summary>
public sealed class JoinRequestRejectedHandler(
    MessagingDbContext dbContext,
    IMessagingUnitOfWork unitOfWork,
    IMessagingInboxStore inboxStore,
    ILogger<JoinRequestRejectedHandler> logger)
    : INotificationHandler<IntegrationEventNotification<JoinRequestRejectedIntegrationEvent>>
{
    public async Task Handle(IntegrationEventNotification<JoinRequestRejectedIntegrationEvent> notification, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(notification);
        if (await inboxStore.HasBeenProcessedAsync(notification.MessageId, ct)) return;
        var evt = notification.Event;

        var body = string.IsNullOrWhiteSpace(evt.Reason)
            ? "Unfortunately your join request was not accepted by the host."
            : $"Your join request was not accepted. Reason: {evt.Reason}";

        dbContext.Notifications.Add(Notification.Create(
            evt.UserId,
            NotificationType.BookingCancelled,
            NotificationChannel.InApp,
            "Join Request Declined",
            body,
            NotificationPriority.Medium,
            entityType: "JoinRequest",
            entityId: evt.JoinRequestId));

        inboxStore.MarkAsProcessed(notification.MessageId);
        await unitOfWork.SaveChangesAsync(ct);
        logger.LogInformation("Queued join request rejected notification for JoinRequest {Id}", evt.JoinRequestId);
    }
}
