using ContentBlogs.Application.Caching;
using ContentBlogs.Domain.Events;
using MediatR;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentBlogs.Infrastructure.EventHandlers;

public sealed class BlogCommentReactionChangedDomainEventHandler(
    HybridCache cache,
    ILogger<BlogCommentReactionChangedDomainEventHandler> logger)
    : INotificationHandler<DomainEventNotification<BlogCommentReactionChangedDomainEvent>>
{
    public async Task Handle(
        DomainEventNotification<BlogCommentReactionChangedDomainEvent> notification,
        CancellationToken cancellationToken)
    {
        var evt = notification.Event;

        await cache
            .RemoveByTagAsync(ContentBlogsCacheKeys.BlogCommentsTag(evt.BlogId), cancellationToken)
            .ConfigureAwait(false);

        logger.LogDebug(
            "BlogCommentReactionChangedDomainEvent: invalidated comments cache for blog {BlogId} " +
            "(CommentId={CommentId}, OldType={OldType}, NewType={NewType}).",
            evt.BlogId, evt.CommentId, evt.OldType, evt.NewType);
    }
}
