using YallaJo.SharedKernel.Domain.Event;

namespace ContentBlogs.Domain.Events;

public sealed record BlogCommentCreatedDomainEvent(
    Guid CommentId,
    Guid BlogId,
    Guid UserId,
    Guid? ParentCommentId,
    DateTime CreatedAtUtc) : DomainEventBase;
