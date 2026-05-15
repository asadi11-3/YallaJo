using ContentBlogs.Contracts.IntegrationEvents;
using ContentBlogs.Domain.Events;
using ContentBlogs.Infrastructure.Persistence;
using MediatR;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Infrastructure.Outbox;

namespace ContentBlogs.Infrastructure.EventHandlers;

public sealed class BlogPublishedDomainEventHandler(
    ContentBlogsDbContext dbContext,
    ILogger<BlogPublishedDomainEventHandler> logger)
    : INotificationHandler<DomainEventNotification<BlogPublishedDomainEvent>>
{
    public Task Handle(
        DomainEventNotification<BlogPublishedDomainEvent> notification,
        CancellationToken cancellationToken)
    {
        var evt = notification.Event;

        dbContext.OutboxMessages.Add(OutboxMessage.Create(
            new BlogPublishedIntegrationEvent(
                BlogId:       evt.BlogId,
                Slug:         evt.Slug,
                Title:        evt.Title,
                AuthorId:     evt.AuthorId,
                PlaceId:      evt.PlaceId,
                PublishedAt:  evt.PublishedAtUtc)));

        logger.LogInformation(
            "BlogPublishedDomainEvent: staged outbox row for blog {BlogId} (Slug={Slug}).",
            evt.BlogId, evt.Slug);

        return Task.CompletedTask;
    }
}
