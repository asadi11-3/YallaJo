using ContentBlogs.Contracts.IntegrationEvents.Creators;
using ContentBlogs.Domain.Events.Creators;
using ContentBlogs.Infrastructure.Persistence;
using MediatR;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Infrastructure.Outbox;

namespace ContentBlogs.Infrastructure.EventHandlers.Creators;

public sealed class CreatorApplicationMoreInfoRequestedDomainEventHandler(
    ContentBlogsDbContext dbContext,
    ILogger<CreatorApplicationMoreInfoRequestedDomainEventHandler> logger)
    : INotificationHandler<DomainEventNotification<CreatorApplicationMoreInfoRequestedDomainEvent>>
{
    public Task Handle(
        DomainEventNotification<CreatorApplicationMoreInfoRequestedDomainEvent> notification,
        CancellationToken cancellationToken)
    {
        var evt = notification.Event;

        dbContext.OutboxMessages.Add(OutboxMessage.Create(
            new CreatorApplicationMoreInfoRequestedIntegrationEvent(
                ApplicationId:    evt.ApplicationId,
                ApplicantUserId:  evt.ApplicantUserId,
                RequestedByAdminId: evt.RequestedByAdminId,
                AdminNote:        evt.AdminNote,
                RequestedAtUtc:   DateTime.UtcNow)));

        logger.LogInformation(
            "CreatorApplicationMoreInfoRequested: staged outbox for application {ApplicationId}.",
            evt.ApplicationId);

        return Task.CompletedTask;
    }
}
