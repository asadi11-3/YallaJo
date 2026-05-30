using ContentBlogs.Contracts.IntegrationEvents;
using ContentBlogs.Domain.Events;
using ContentBlogs.Infrastructure.Persistence;
using MediatR;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Infrastructure.Outbox;

namespace ContentBlogs.Infrastructure.EventHandlers;

public sealed class BlogDeletedDomainEventHandler(
    ContentBlogsDbContext dbContext,
    ILogger<BlogDeletedDomainEventHandler> logger)
    : INotificationHandler<DomainEventNotification<BlogDeletedDomainEvent>>
{
    public Task Handle(
        DomainEventNotification<BlogDeletedDomainEvent> notification,
        CancellationToken cancellationToken)
    {
        var evt = notification.Event;

        dbContext.OutboxMessages.Add(OutboxMessage.Create(
            new BlogDeletedIntegrationEvent(
                BlogId:    evt.BlogId,
                Slug:      evt.Slug,
                DeletedAt: evt.DeletedAtUtc)));

        logger.LogInformation(
            "BlogDeletedDomainEvent: staged outbox row for blog {BlogId} (Slug={Slug}).",
            evt.BlogId, evt.Slug);

        return Task.CompletedTask;
    }
}
