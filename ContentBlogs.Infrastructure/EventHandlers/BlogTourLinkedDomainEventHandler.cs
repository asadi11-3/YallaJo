using ContentBlogs.Contracts.IntegrationEvents;
using ContentBlogs.Domain.Events;
using ContentBlogs.Infrastructure.Persistence;
using MediatR;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Infrastructure.Outbox;

namespace ContentBlogs.Infrastructure.EventHandlers;

public sealed class BlogTourLinkedDomainEventHandler(
    ContentBlogsDbContext dbContext,
    ILogger<BlogTourLinkedDomainEventHandler> logger)
    : INotificationHandler<DomainEventNotification<BlogTourLinkedDomainEvent>>
{
    public Task Handle(
        DomainEventNotification<BlogTourLinkedDomainEvent> notification,
        CancellationToken cancellationToken)
    {
        var evt = notification.Event;

        dbContext.OutboxMessages.Add(OutboxMessage.Create(
            new BlogTourLinkedIntegrationEvent(
                BlogId:   evt.BlogId,
                TourId:   evt.TourId,
                LinkedAt: evt.LinkedAtUtc)));

        logger.LogInformation(
            "BlogTourLinkedDomainEvent: staged outbox row for blog {BlogId} linked to tour {TourId}.",
            evt.BlogId, evt.TourId);

        return Task.CompletedTask;
    }
}
