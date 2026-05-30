using YallaJo.SharedKernel.Domain.Event;

namespace Social.Domain.Events;

/// <summary>Raised when a review is soft-deleted (by user, admin, or system).</summary>
public sealed record ReviewDeletedDomainEvent(
    Guid ReviewId, Guid UserId,
    Social.Domain.Enums.ReviewTargetType TargetType, Guid TargetId,
    DateTime DeletedAt, Social.Domain.Enums.ReviewDeletionSource Source) : DomainEventBase;
