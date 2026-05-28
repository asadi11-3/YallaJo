using ContentBlogs.Contracts.IntegrationEvents.Creators;
using ContentBlogs.Domain.Events.Creators;
using ContentBlogs.Infrastructure.Persistence;
using MediatR;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Infrastructure.Outbox;

namespace ContentBlogs.Infrastructure.EventHandlers.Creators;

public sealed class CreatorApplicationApprovedDomainEventHandler(
    ContentBlogsDbContext dbContext,
    ILogger<CreatorApplicationApprovedDomainEventHandler> logger)
    : INotificationHandler<DomainEventNotification<CreatorApplicationApprovedDomainEvent>>
{
    public Task Handle(
        DomainEventNotification<CreatorApplicationApprovedDomainEvent> notification,
        CancellationToken cancellationToken)
    {
        var evt = notification.Event;

        dbContext.OutboxMessages.Add(OutboxMessage.Create(
            new CreatorApplicationApprovedIntegrationEvent(
                ApplicationId:   evt.ApplicationId,
                ApplicantUserId: evt.ApplicantUserId,
                ApprovedByAdminId: evt.ApprovedByAdminId,
                ApprovedAtUtc:   DateTime.UtcNow)));

        logger.LogInformation(
            "CreatorApplicationApproved: staged outbox for application {ApplicationId}.",
            evt.ApplicationId);

        return Task.CompletedTask;
    }
}
