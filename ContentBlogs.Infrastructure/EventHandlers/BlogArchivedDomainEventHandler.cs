using ContentBlogs.Contracts.IntegrationEvents;
using ContentBlogs.Domain.Events;
using ContentBlogs.Infrastructure.Persistence;
using MediatR;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Infrastructure.Outbox;

namespace ContentBlogs.Infrastructure.EventHandlers;

public sealed class BlogArchivedDomainEventHandler(
    ContentBlogsDbContext dbContext,
    ILogger<BlogArchivedDomainEventHandler> logger)
    : INotificationHandler<DomainEventNotification<BlogArchivedDomainEvent>>
{
    public Task Handle(
        DomainEventNotification<BlogArchivedDomainEvent> notification,
        CancellationToken cancellationToken)
    {
        var evt = notification.Event;

        dbContext.OutboxMessages.Add(OutboxMessage.Create(
            new BlogArchivedIntegrationEvent(
                BlogId:     evt.BlogId,
                Slug:       evt.Slug,
                ArchivedAt: evt.ArchivedAtUtc)));

        logger.LogInformation(
            "BlogArchivedDomainEvent: staged outbox row for blog {BlogId} (Slug={Slug}).",
            evt.BlogId, evt.Slug);

        return Task.CompletedTask;
    }
}
