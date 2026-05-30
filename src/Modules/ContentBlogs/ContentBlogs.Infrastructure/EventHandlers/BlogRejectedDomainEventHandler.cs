using ContentBlogs.Contracts.IntegrationEvents;
using ContentBlogs.Domain.Events;
using ContentBlogs.Infrastructure.Persistence;
using MediatR;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Infrastructure.Outbox;

namespace ContentBlogs.Infrastructure.EventHandlers;

public sealed class BlogRejectedDomainEventHandler(
    ContentBlogsDbContext dbContext,
    ILogger<BlogRejectedDomainEventHandler> logger)
    : INotificationHandler<DomainEventNotification<BlogRejectedDomainEvent>>
{
    public Task Handle(
        DomainEventNotification<BlogRejectedDomainEvent> notification,
        CancellationToken cancellationToken)
    {
        var evt = notification.Event;

        dbContext.OutboxMessages.Add(OutboxMessage.Create(
            new BlogRejectedIntegrationEvent(
                BlogId:                     evt.BlogId,
                Slug:                       evt.Slug,
                Title:                      string.Empty,
                AuthorId:                   Guid.Empty,
                AuthoredByCreatorProfileId: evt.AuthoredByCreatorId,
                RejectedByAdminId:          evt.RejectedByAdminId,
                Reason:                     evt.Reason,
                RejectedAt:                 evt.RejectedAtUtc)));

        logger.LogInformation(
            "BlogRejectedDomainEvent: staged outbox row for blog {BlogId} (Slug={Slug}).",
            evt.BlogId, evt.Slug);

        return Task.CompletedTask;
    }
}
