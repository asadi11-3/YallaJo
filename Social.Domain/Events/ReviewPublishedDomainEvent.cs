using YallaJo.SharedKernel.Domain.Event;

namespace Social.Domain.Events;

/// <summary>Raised when a review passes all checks and becomes publicly visible.</summary>
public sealed record ReviewPublishedDomainEvent(
    Guid ReviewId, Guid UserId,
    Social.Domain.Enums.ReviewTargetType TargetType, Guid TargetId,
    decimal Rating, DateTime PublishedAt) : DomainEventBase;
