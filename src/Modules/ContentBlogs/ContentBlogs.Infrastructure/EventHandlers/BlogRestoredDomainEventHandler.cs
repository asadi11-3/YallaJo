using ContentBlogs.Contracts.IntegrationEvents;
using ContentBlogs.Domain.Events;
using ContentBlogs.Infrastructure.Persistence;
using MediatR;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Infrastructure.Outbox;

namespace ContentBlogs.Infrastructure.EventHandlers;

public sealed class BlogRestoredDomainEventHandler(
    ContentBlogsDbContext dbContext,
    ILogger<BlogRestoredDomainEventHandler> logger)
    : INotificationHandler<DomainEventNotification<BlogRestoredDomainEvent>>
{
    public Task Handle(
        DomainEventNotification<BlogRestoredDomainEvent> notification,
        CancellationToken cancellationToken)
    {
        var evt = notification.Event;

        dbContext.OutboxMessages.Add(OutboxMessage.Create(
            new BlogRestoredIntegrationEvent(
                BlogId:     evt.BlogId,
                Slug:       evt.Slug,
                RestoredAt: evt.RestoredAtUtc)));

        logger.LogInformation(
            "BlogRestoredDomainEvent: staged outbox row for blog {BlogId} (Slug={Slug}).",
            evt.BlogId, evt.Slug);

        return Task.CompletedTask;
    }
}
