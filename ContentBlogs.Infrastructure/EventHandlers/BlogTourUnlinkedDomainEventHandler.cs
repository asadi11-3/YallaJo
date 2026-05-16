using ContentBlogs.Contracts.IntegrationEvents;
using ContentBlogs.Domain.Events;
using ContentBlogs.Infrastructure.Persistence;
using MediatR;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Infrastructure.Outbox;

namespace ContentBlogs.Infrastructure.EventHandlers;

public sealed class BlogTourUnlinkedDomainEventHandler(
    ContentBlogsDbContext dbContext,
    ILogger<BlogTourUnlinkedDomainEventHandler> logger)
    : INotificationHandler<DomainEventNotification<BlogTourUnlinkedDomainEvent>>
{
    public Task Handle(
        DomainEventNotification<BlogTourUnlinkedDomainEvent> notification,
        CancellationToken cancellationToken)
    {
        var evt = notification.Event;

        dbContext.OutboxMessages.Add(OutboxMessage.Create(
            new BlogTourUnlinkedIntegrationEvent(
                BlogId:     evt.BlogId,
                TourId:     evt.TourId,
                UnlinkedAt: evt.UnlinkedAtUtc)));

        logger.LogInformation(
            "BlogTourUnlinkedDomainEvent: staged outbox row for blog {BlogId} unlinked from tour {TourId}.",
            evt.BlogId, evt.TourId);

        return Task.CompletedTask;
    }
}
