using ContentBlogs.Domain.Enums;
using YallaJo.SharedKernel.Domain.Event;

namespace ContentBlogs.Domain.Events;

/// <summary>
/// Raised whenever a user's reaction on a <c>BlogComment</c> transitions between
/// states. A single event covers all three transitions:
/// <list type="bullet">
///   <item><description>Add: <see cref="OldType"/> = <c>null</c>, <see cref="NewType"/> = X.</description></item>
///   <item><description>Replace: <see cref="OldType"/> = Y, <see cref="NewType"/> = X.</description></item>
///   <item><description>Remove: <see cref="OldType"/> = X, <see cref="NewType"/> = <c>null</c>.</description></item>
/// </list>
/// </summary>
public sealed record BlogCommentReactionChangedDomainEvent(
    Guid CommentId,
    Guid BlogId,
    Guid UserId,
    ReactionType? OldType,
    ReactionType? NewType,
    DateTime OccurredAtUtc) : DomainEventBase;
