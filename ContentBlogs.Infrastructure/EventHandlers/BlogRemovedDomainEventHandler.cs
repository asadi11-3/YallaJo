using ContentBlogs.Contracts.IntegrationEvents;
using ContentBlogs.Domain.Events;
using ContentBlogs.Infrastructure.Persistence;
using MediatR;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Infrastructure.Outbox;

namespace ContentBlogs.Infrastructure.EventHandlers;

public sealed class BlogRemovedDomainEventHandler(
    ContentBlogsDbContext dbContext,
    ILogger<BlogRemovedDomainEventHandler> logger)
    : INotificationHandler<DomainEventNotification<BlogRemovedDomainEvent>>
{
    public Task Handle(
        DomainEventNotification<BlogRemovedDomainEvent> notification,
        CancellationToken cancellationToken)
    {
        var evt = notification.Event;

        dbContext.OutboxMessages.Add(OutboxMessage.Create(
            new BlogRemovedIntegrationEvent(
                BlogId:                     evt.BlogId,
                Slug:                       evt.Slug,
                Title:                      string.Empty,
                AuthorId:                   Guid.Empty,
                AuthoredByCreatorProfileId: null,
                RemovedByAdminId:           evt.RemovedByAdminId,
                Reason:                     evt.Reason,
                RemovedAt:                  evt.RemovedAtUtc)));

        logger.LogInformation(
            "BlogRemovedDomainEvent: staged outbox row for blog {BlogId} (Slug={Slug}).",
            evt.BlogId, evt.Slug);

        return Task.CompletedTask;
    }
}
