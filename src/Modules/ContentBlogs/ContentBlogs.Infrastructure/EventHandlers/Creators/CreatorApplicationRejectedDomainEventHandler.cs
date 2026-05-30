using ContentBlogs.Contracts.IntegrationEvents.Creators;
using ContentBlogs.Domain.Events.Creators;
using ContentBlogs.Infrastructure.Persistence;
using MediatR;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Infrastructure.Outbox;

namespace ContentBlogs.Infrastructure.EventHandlers.Creators;

public sealed class CreatorApplicationRejectedDomainEventHandler(
    ContentBlogsDbContext dbContext,
    ILogger<CreatorApplicationRejectedDomainEventHandler> logger)
    : INotificationHandler<DomainEventNotification<CreatorApplicationRejectedDomainEvent>>
{
    public Task Handle(
        DomainEventNotification<CreatorApplicationRejectedDomainEvent> notification,
        CancellationToken cancellationToken)
    {
        var evt = notification.Event;

        dbContext.OutboxMessages.Add(OutboxMessage.Create(
            new CreatorApplicationRejectedIntegrationEvent(
                ApplicationId:   evt.ApplicationId,
                ApplicantUserId: evt.ApplicantUserId,
                RejectedByAdminId: evt.RejectedByAdminId,
                Reason:          evt.Reason,
                RejectedAtUtc:   DateTime.UtcNow)));

        logger.LogInformation(
            "CreatorApplicationRejected: staged outbox for application {ApplicationId}.",
            evt.ApplicationId);

        return Task.CompletedTask;
    }
}
