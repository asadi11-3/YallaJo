using YallaJo.SharedKernel.Domain.Event;

namespace ContentBlogs.Domain.Events;

/// <summary>
/// Raised when a <c>BlogComment</c> is soft-deleted (content redacted).
/// The row is preserved so replies remain visible in the thread; the content
/// is replaced with the redaction marker.
/// </summary>
public sealed record BlogCommentDeletedDomainEvent(
    Guid CommentId,
    Guid BlogId,
    Guid UserId,
    DateTime DeletedAtUtc) : DomainEventBase;
