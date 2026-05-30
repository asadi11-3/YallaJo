using YallaJo.SharedKernel.Domain.Event;

namespace ContentBlogs.Domain.Events;

public sealed record BlogCommentUpdatedDomainEvent(
    Guid CommentId,
    Guid BlogId,
    Guid UserId,
    DateTime UpdatedAtUtc) : DomainEventBase;
