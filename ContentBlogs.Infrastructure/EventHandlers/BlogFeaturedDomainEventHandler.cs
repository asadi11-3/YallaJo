using ContentBlogs.Contracts.IntegrationEvents;
using ContentBlogs.Domain.Events;
using ContentBlogs.Infrastructure.Persistence;
using MediatR;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Infrastructure.Outbox;

namespace ContentBlogs.Infrastructure.EventHandlers;

public sealed class BlogFeaturedDomainEventHandler(
    ContentBlogsDbContext dbContext,
    ILogger<BlogFeaturedDomainEventHandler> logger)
    : INotificationHandler<DomainEventNotification<BlogFeaturedDomainEvent>>
{
    public Task Handle(
        DomainEventNotification<BlogFeaturedDomainEvent> notification,
        CancellationToken cancellationToken)
    {
        var evt = notification.Event;

        dbContext.OutboxMessages.Add(OutboxMessage.Create(
            new BlogFeaturedIntegrationEvent(
                BlogId:     evt.BlogId,
                Slug:       evt.Slug,
                Title:      evt.Title,
                AuthorId:   evt.AuthorId,
                PlaceId:    evt.PlaceId,
                FeaturedAt: evt.FeaturedAtUtc)));

        logger.LogInformation(
            "BlogFeaturedDomainEvent: staged outbox row for blog {BlogId} (Slug={Slug}, PlaceId={PlaceId}).",
            evt.BlogId, evt.Slug, evt.PlaceId);

        return Task.CompletedTask;
    }
}
