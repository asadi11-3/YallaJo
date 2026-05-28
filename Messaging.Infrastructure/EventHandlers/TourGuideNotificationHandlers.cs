using Accounts.Contracts.IntegrationEvents;
using ContentTours.Contracts;
using MediatR;
using Messaging.Application.Interfaces;
using Messaging.Domain.Entities;
using Messaging.Domain.Enums;
using Messaging.Infrastructure.Persistence;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Messaging.Infrastructure.EventHandlers;

public sealed class GuideApplicationApprovedNotificationHandler(
    MessagingDbContext dbContext,
    IMessagingUnitOfWork unitOfWork,
    IMessagingInboxStore inboxStore,
    ILogger<GuideApplicationApprovedNotificationHandler> logger)
    : INotificationHandler<IntegrationEventNotification<GuideApplicationApprovedIntegrationEvent>>
{
    public async Task Handle(IntegrationEventNotification<GuideApplicationApprovedIntegrationEvent> notification, CancellationToken ct)
    {
        if (await inboxStore.HasBeenProcessedAsync(notification.MessageId, ct)) return;
        var evt = notification.Event;

        dbContext.Notifications.Add(Notification.Create(evt.GuideUserId, NotificationType.GuideApplicationApproved, NotificationChannel.InApp,
            "Guide application approved", "Your guide application has been approved.", NotificationPriority.High,
            entityType: "GuideApplication", entityId: evt.ApplicationId));

        inboxStore.MarkAsProcessed(notification.MessageId);
        await unitOfWork.SaveChangesAsync(ct);
        logger.LogInformation("Queued guide application approved notification for application {ApplicationId}", evt.ApplicationId);
    }
}

public sealed class GuideApplicationRejectedNotificationHandler(
    MessagingDbContext dbContext,
    IMessagingUnitOfWork unitOfWork,
    IMessagingInboxStore inboxStore,
    ILogger<GuideApplicationRejectedNotificationHandler> logger)
    : INotificationHandler<IntegrationEventNotification<GuideApplicationRejectedIntegrationEvent>>
{
    public async Task Handle(IntegrationEventNotification<GuideApplicationRejectedIntegrationEvent> notification, CancellationToken ct)
    {
        if (await inboxStore.HasBeenProcessedAsync(notification.MessageId, ct)) return;
        var evt = notification.Event;

        dbContext.Notifications.Add(Notification.Create(evt.GuideUserId, NotificationType.GuideApplicationRejected, NotificationChannel.InApp,
            "Guide application rejected", $"Your guide application was rejected. Reason: {evt.Reason}", NotificationPriority.Medium,
            entityType: "GuideApplication", entityId: evt.ApplicationId));

        inboxStore.MarkAsProcessed(notification.MessageId);
        await unitOfWork.SaveChangesAsync(ct);
        logger.LogInformation("Queued guide application rejected notification for application {ApplicationId}", evt.ApplicationId);
    }
}

public sealed class TourProposalApprovedNotificationHandler(
    MessagingDbContext dbContext,
    IMessagingUnitOfWork unitOfWork,
    IMessagingInboxStore inboxStore,
    ILogger<TourProposalApprovedNotificationHandler> logger)
    : INotificationHandler<IntegrationEventNotification<TourProposalApprovedIntegrationEvent>>
{
    public async Task Handle(IntegrationEventNotification<TourProposalApprovedIntegrationEvent> notification, CancellationToken ct)
    {
        if (await inboxStore.HasBeenProcessedAsync(notification.MessageId, ct)) return;
        var evt = notification.Event;

        dbContext.Notifications.Add(Notification.Create(evt.GuideUserId, NotificationType.TourProposalApproved, NotificationChannel.InApp,
            "Tour proposal approved", "Your tour proposal has been approved.", NotificationPriority.High,
            entityType: "TourProposal", entityId: evt.ProposalId));

        inboxStore.MarkAsProcessed(notification.MessageId);
        await unitOfWork.SaveChangesAsync(ct);
        logger.LogInformation("Queued tour proposal approved notification for proposal {ProposalId}", evt.ProposalId);
    }
}

public sealed class TourProposalRejectedNotificationHandler(
    MessagingDbContext dbContext,
    IMessagingUnitOfWork unitOfWork,
    IMessagingInboxStore inboxStore,
    ILogger<TourProposalRejectedNotificationHandler> logger)
    : INotificationHandler<IntegrationEventNotification<TourProposalRejectedIntegrationEvent>>
{
    public async Task Handle(IntegrationEventNotification<TourProposalRejectedIntegrationEvent> notification, CancellationToken ct)
    {
        if (await inboxStore.HasBeenProcessedAsync(notification.MessageId, ct)) return;
        var evt = notification.Event;

        dbContext.Notifications.Add(Notification.Create(evt.GuideUserId, NotificationType.TourProposalRejected, NotificationChannel.InApp,
            "Tour proposal rejected", $"Your tour proposal was rejected. Reason: {evt.Reason}", NotificationPriority.Medium,
            entityType: "TourProposal", entityId: evt.ProposalId));

        inboxStore.MarkAsProcessed(notification.MessageId);
        await unitOfWork.SaveChangesAsync(ct);
        logger.LogInformation("Queued tour proposal rejected notification for proposal {ProposalId}", evt.ProposalId);
    }
}

public sealed class NewGuideApplicationNotificationHandler(
    MessagingDbContext dbContext,
    IMessagingUnitOfWork unitOfWork,
    IMessagingInboxStore inboxStore,
    ILogger<NewGuideApplicationNotificationHandler> logger)
    : INotificationHandler<IntegrationEventNotification<NewGuideApplicationIntegrationEvent>>
{
    public async Task Handle(IntegrationEventNotification<NewGuideApplicationIntegrationEvent> notification, CancellationToken ct)
    {
        if (await inboxStore.HasBeenProcessedAsync(notification.MessageId, ct)) return;
        var evt = notification.Event;

        dbContext.Notifications.Add(Notification.Create(evt.TourOwnerUserId, NotificationType.NewGuideApplication, NotificationChannel.InApp,
            "New guide application", "A tour guide applied to join your tour.", NotificationPriority.Medium,
            entityType: "GuideApplication", entityId: evt.ApplicationId));

        inboxStore.MarkAsProcessed(notification.MessageId);
        await unitOfWork.SaveChangesAsync(ct);
        logger.LogInformation("Queued new guide application notification for application {ApplicationId}", evt.ApplicationId);
    }
}

public sealed class AgencyAffiliationCreatedNotificationHandler(
    MessagingDbContext dbContext,
    IMessagingUnitOfWork unitOfWork,
    IMessagingInboxStore inboxStore,
    ILogger<AgencyAffiliationCreatedNotificationHandler> logger)
    : INotificationHandler<IntegrationEventNotification<AgencyAffiliationCreatedIntegrationEvent>>
{
    public async Task Handle(IntegrationEventNotification<AgencyAffiliationCreatedIntegrationEvent> notification, CancellationToken ct)
    {
        if (await inboxStore.HasBeenProcessedAsync(notification.MessageId, ct)) return;
        var evt = notification.Event;

        dbContext.Notifications.Add(Notification.Create(evt.GuideUserId, NotificationType.AgencyAffiliationCreated, NotificationChannel.InApp,
            "Agency affiliation created", "An agency affiliation has been created for your guide profile.", NotificationPriority.Medium,
            entityType: "AgencyAffiliation", entityId: evt.AffiliationId));

        inboxStore.MarkAsProcessed(notification.MessageId);
        await unitOfWork.SaveChangesAsync(ct);
        logger.LogInformation("Queued agency affiliation created notification for affiliation {AffiliationId}", evt.AffiliationId);
    }
}
