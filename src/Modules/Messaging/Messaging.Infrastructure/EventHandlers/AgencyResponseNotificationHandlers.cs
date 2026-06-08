using Accounts.Contracts.IntegrationEvents;
using MediatR;
using Messaging.Application.Interfaces;
using Messaging.Domain.Entities;
using Messaging.Domain.Enums;
using Messaging.Infrastructure.Persistence;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Messaging.Infrastructure.EventHandlers;

public sealed class AgencyApplicationApprovedNotificationHandler(
    MessagingDbContext dbContext,
    IMessagingUnitOfWork unitOfWork,
    IMessagingInboxStore inboxStore,
    ILogger<AgencyApplicationApprovedNotificationHandler> logger)
    : INotificationHandler<IntegrationEventNotification<AgencyApplicationApprovedIntegrationEvent>>
{
    public async Task Handle(IntegrationEventNotification<AgencyApplicationApprovedIntegrationEvent> notification, CancellationToken ct)
    {
        if (await inboxStore.HasBeenProcessedAsync(notification.MessageId, ct)) return;
        var evt = notification.Event;

        dbContext.Notifications.Add(Notification.Create(evt.GuideUserId, NotificationType.AgencyApplicationApproved, NotificationChannel.InApp,
            "Agency application approved", "Your application to join the agency was approved.", NotificationPriority.Medium,
            entityType: "AgencyApplication", entityId: evt.ApplicationId));

        inboxStore.MarkAsProcessed(notification.MessageId);
        await unitOfWork.SaveChangesAsync(ct);
        logger.LogInformation("Queued agency application approved notification for application {ApplicationId}", evt.ApplicationId);
    }
}

public sealed class AgencyApplicationRejectedNotificationHandler(
    MessagingDbContext dbContext,
    IMessagingUnitOfWork unitOfWork,
    IMessagingInboxStore inboxStore,
    ILogger<AgencyApplicationRejectedNotificationHandler> logger)
    : INotificationHandler<IntegrationEventNotification<AgencyApplicationRejectedIntegrationEvent>>
{
    public async Task Handle(IntegrationEventNotification<AgencyApplicationRejectedIntegrationEvent> notification, CancellationToken ct)
    {
        if (await inboxStore.HasBeenProcessedAsync(notification.MessageId, ct)) return;
        var evt = notification.Event;

        dbContext.Notifications.Add(Notification.Create(evt.GuideUserId, NotificationType.AgencyApplicationRejected, NotificationChannel.InApp,
            "Agency application declined", $"Your application to join the agency was declined. Reason: {evt.Reason}", NotificationPriority.Medium,
            entityType: "AgencyApplication", entityId: evt.ApplicationId));

        inboxStore.MarkAsProcessed(notification.MessageId);
        await unitOfWork.SaveChangesAsync(ct);
        logger.LogInformation("Queued agency application rejected notification for application {ApplicationId}", evt.ApplicationId);
    }
}

public sealed class AgencyInvitationAcceptedNotificationHandler(
    MessagingDbContext dbContext,
    IMessagingUnitOfWork unitOfWork,
    IMessagingInboxStore inboxStore,
    ILogger<AgencyInvitationAcceptedNotificationHandler> logger)
    : INotificationHandler<IntegrationEventNotification<AgencyInvitationAcceptedIntegrationEvent>>
{
    public async Task Handle(IntegrationEventNotification<AgencyInvitationAcceptedIntegrationEvent> notification, CancellationToken ct)
    {
        if (await inboxStore.HasBeenProcessedAsync(notification.MessageId, ct)) return;
        var evt = notification.Event;

        dbContext.Notifications.Add(Notification.Create(evt.AgencyUserId, NotificationType.AgencyInvitationAccepted, NotificationChannel.InApp,
            "Invitation accepted", "A guide accepted your agency invitation.", NotificationPriority.Medium,
            entityType: "AgencyInvitation", entityId: evt.InvitationId));

        inboxStore.MarkAsProcessed(notification.MessageId);
        await unitOfWork.SaveChangesAsync(ct);
        logger.LogInformation("Queued agency invitation accepted notification for invitation {InvitationId}", evt.InvitationId);
    }
}

public sealed class AgencyInvitationDeclinedNotificationHandler(
    MessagingDbContext dbContext,
    IMessagingUnitOfWork unitOfWork,
    IMessagingInboxStore inboxStore,
    ILogger<AgencyInvitationDeclinedNotificationHandler> logger)
    : INotificationHandler<IntegrationEventNotification<AgencyInvitationDeclinedIntegrationEvent>>
{
    public async Task Handle(IntegrationEventNotification<AgencyInvitationDeclinedIntegrationEvent> notification, CancellationToken ct)
    {
        if (await inboxStore.HasBeenProcessedAsync(notification.MessageId, ct)) return;
        var evt = notification.Event;

        dbContext.Notifications.Add(Notification.Create(evt.AgencyUserId, NotificationType.AgencyInvitationDeclined, NotificationChannel.InApp,
            "Invitation declined", "A guide declined your agency invitation.", NotificationPriority.Medium,
            entityType: "AgencyInvitation", entityId: evt.InvitationId));

        inboxStore.MarkAsProcessed(notification.MessageId);
        await unitOfWork.SaveChangesAsync(ct);
        logger.LogInformation("Queued agency invitation declined notification for invitation {InvitationId}", evt.InvitationId);
    }
}
