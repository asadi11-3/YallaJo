using ContentBlogs.Contracts.IntegrationEvents;
using ContentBlogs.Domain.Events;
using ContentBlogs.Infrastructure.Persistence;
using MediatR;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Infrastructure.Outbox;

namespace ContentBlogs.Infrastructure.EventHandlers;

public sealed class BlogUnfeaturedDomainEventHandler(
    ContentBlogsDbContext dbContext,
    ILogger<BlogUnfeaturedDomainEventHandler> logger)
    : INotificationHandler<DomainEventNotification<BlogUnfeaturedDomainEvent>>
{
    public Task Handle(
        DomainEventNotification<BlogUnfeaturedDomainEvent> notification,
        CancellationToken cancellationToken)
    {
        var evt = notification.Event;

        dbContext.OutboxMessages.Add(OutboxMessage.Create(
            new BlogUnfeaturedIntegrationEvent(
                BlogId:       evt.BlogId,
                Slug:         evt.Slug,
                PlaceId:      evt.PlaceId,
                UnfeaturedAt: evt.UnfeaturedAtUtc)));

        logger.LogInformation(
            "BlogUnfeaturedDomainEvent: staged outbox row for blog {BlogId} (Slug={Slug}, PlaceId={PlaceId}).",
            evt.BlogId, evt.Slug, evt.PlaceId);

        return Task.CompletedTask;
    }
}
