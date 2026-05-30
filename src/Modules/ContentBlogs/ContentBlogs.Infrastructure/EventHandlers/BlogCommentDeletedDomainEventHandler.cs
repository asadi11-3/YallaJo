using ContentBlogs.Application.Caching;
using ContentBlogs.Domain.Events;
using MediatR;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentBlogs.Infrastructure.EventHandlers;

public sealed class BlogCommentDeletedDomainEventHandler(
    HybridCache cache,
    ILogger<BlogCommentDeletedDomainEventHandler> logger)
    : INotificationHandler<DomainEventNotification<BlogCommentDeletedDomainEvent>>
{
    public async Task Handle(
        DomainEventNotification<BlogCommentDeletedDomainEvent> notification,
        CancellationToken cancellationToken)
    {
        var evt = notification.Event;

        await cache
            .RemoveByTagAsync(ContentBlogsCacheKeys.BlogCommentsTag(evt.BlogId), cancellationToken)
            .ConfigureAwait(false);

        logger.LogDebug(
            "BlogCommentDeletedDomainEvent: invalidated comments cache for blog {BlogId}.",
            evt.BlogId);
    }
}
