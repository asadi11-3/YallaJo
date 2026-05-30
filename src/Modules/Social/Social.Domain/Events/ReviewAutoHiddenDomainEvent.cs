using YallaJo.SharedKernel.Domain.Event;

namespace Social.Domain.Events;

/// <summary>Raised when a review is auto-hidden after reaching the report threshold (S-R5).</summary>
public sealed record ReviewAutoHiddenDomainEvent(
    Guid ReviewId,
    Social.Domain.Enums.ReviewTargetType TargetType, Guid TargetId,
    int ReportCount, DateTime AutoHiddenAt) : DomainEventBase;
