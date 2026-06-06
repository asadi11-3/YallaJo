using Social.Domain.Enums;
using YallaJo.SharedKernel.Domain.Event;

namespace Social.Domain.Events;

/// <summary>
/// Raised when an accessibility review is first submitted (S-AR Phase-3 WS-2).
/// </summary>
public sealed record AccessibilityReviewSubmittedDomainEvent(
    Guid ReviewId,
    Guid UserId,
    ReviewTargetType TargetType,
    Guid TargetId,
    decimal Rating,
    string FeatureTypesCsv,
    DateTime OccurredAt) : DomainEventBase;
