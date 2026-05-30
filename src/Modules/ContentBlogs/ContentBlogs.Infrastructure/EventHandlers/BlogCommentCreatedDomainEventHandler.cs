using ContentBlogs.Application.Caching;
using ContentBlogs.Domain.Events;
using MediatR;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentBlogs.Infrastructure.EventHandlers;

public sealed class BlogCommentCreatedDomainEventHandler(
    HybridCache cache,
    ILogger<BlogCommentCreatedDomainEventHandler> logger)
    : INotificationHandler<DomainEventNotification<BlogCommentCreatedDomainEvent>>
{
    public async Task Handle(
        DomainEventNotification<BlogCommentCreatedDomainEvent> notification,
        CancellationToken cancellationToken)
    {
        var evt = notification.Event;

        await cache
            .RemoveByTagAsync(ContentBlogsCacheKeys.BlogCommentsTag(evt.BlogId), cancellationToken)
            .ConfigureAwait(false);

        logger.LogDebug(
            "BlogCommentCreatedDomainEvent: invalidated comments cache for blog {BlogId}.",
            evt.BlogId);
    }
}
