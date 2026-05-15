using ContentBlogs.Contracts.IntegrationEvents;
using ContentBlogs.Domain.Events;
using ContentBlogs.Infrastructure.Persistence;
using MediatR;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Infrastructure.Outbox;

namespace ContentBlogs.Infrastructure.EventHandlers;

public sealed class BlogUnpublishedDomainEventHandler(
    ContentBlogsDbContext dbContext,
    ILogger<BlogUnpublishedDomainEventHandler> logger)
    : INotificationHandler<DomainEventNotification<BlogUnpublishedDomainEvent>>
{
    public Task Handle(
        DomainEventNotification<BlogUnpublishedDomainEvent> notification,
        CancellationToken cancellationToken)
    {
        var evt = notification.Event;

        dbContext.OutboxMessages.Add(OutboxMessage.Create(
            new BlogUnpublishedIntegrationEvent(
                BlogId:        evt.BlogId,
                Slug:          evt.Slug,
                UnpublishedAt: evt.UnpublishedAtUtc)));

        logger.LogInformation(
            "BlogUnpublishedDomainEvent: staged outbox row for blog {BlogId} (Slug={Slug}).",
            evt.BlogId, evt.Slug);

        return Task.CompletedTask;
    }
}
