using ContentBlogs.Contracts.IntegrationEvents.Creators;
using ContentBlogs.Domain.Events.Creators;
using ContentBlogs.Infrastructure.Persistence;
using MediatR;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Infrastructure.Outbox;

namespace ContentBlogs.Infrastructure.EventHandlers.Creators;

public sealed class CreatorApplicationSubmittedDomainEventHandler(
    ContentBlogsDbContext dbContext,
    ILogger<CreatorApplicationSubmittedDomainEventHandler> logger)
    : INotificationHandler<DomainEventNotification<CreatorApplicationSubmittedDomainEvent>>
{
    public Task Handle(
        DomainEventNotification<CreatorApplicationSubmittedDomainEvent> notification,
        CancellationToken cancellationToken)
    {
        var evt = notification.Event;

        dbContext.OutboxMessages.Add(OutboxMessage.Create(
            new CreatorApplicationSubmittedIntegrationEvent(
                ApplicationId:  evt.ApplicationId,
                ApplicantUserId: evt.ApplicantUserId,
                SubmittedAtUtc: DateTime.UtcNow)));

        logger.LogInformation(
            "CreatorApplicationSubmitted: staged outbox for application {ApplicationId}.",
            evt.ApplicationId);

        return Task.CompletedTask;
    }
}
