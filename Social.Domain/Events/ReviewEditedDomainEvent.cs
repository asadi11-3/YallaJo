using YallaJo.SharedKernel.Domain.Event;

namespace Social.Domain.Events;

/// <summary>Raised when the author edits their review within the 48-hour window.</summary>
public sealed record ReviewEditedDomainEvent(
    Guid ReviewId, Guid UserId,
    decimal OldRating, decimal NewRating, DateTime EditedAt) : DomainEventBase;
