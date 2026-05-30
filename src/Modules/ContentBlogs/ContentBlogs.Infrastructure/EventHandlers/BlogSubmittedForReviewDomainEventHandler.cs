using ContentBlogs.Contracts.IntegrationEvents;
using ContentBlogs.Domain.Events;
using ContentBlogs.Infrastructure.Persistence;
using MediatR;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Infrastructure.Outbox;

namespace ContentBlogs.Infrastructure.EventHandlers;

public sealed class BlogSubmittedForReviewDomainEventHandler(
    ContentBlogsDbContext dbContext,
    ILogger<BlogSubmittedForReviewDomainEventHandler> logger)
    : INotificationHandler<DomainEventNotification<BlogSubmittedForReviewDomainEvent>>
{
    public Task Handle(
        DomainEventNotification<BlogSubmittedForReviewDomainEvent> notification,
        CancellationToken cancellationToken)
    {
        var evt = notification.Event;

        dbContext.OutboxMessages.Add(OutboxMessage.Create(
            new BlogSubmittedForReviewIntegrationEvent(
                BlogId:                    evt.BlogId,
                Slug:                      evt.Slug,
                Title:                      string.Empty,
                AuthorId:                   Guid.Empty,
                AuthoredByCreatorProfileId: evt.CreatorProfileId,
                SubmittedAt:               evt.SubmittedAtUtc)));

        logger.LogInformation(
            "BlogSubmittedForReviewDomainEvent: staged outbox row for blog {BlogId} (Slug={Slug}).",
            evt.BlogId, evt.Slug);

        return Task.CompletedTask;
    }
}
