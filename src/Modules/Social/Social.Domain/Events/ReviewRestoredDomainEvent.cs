using YallaJo.SharedKernel.Domain.Event;

namespace Social.Domain.Events;

/// <summary>Raised when an admin restores a previously auto-hidden review.</summary>
public sealed record ReviewRestoredDomainEvent(
    Guid ReviewId, Guid RestoredByUserId, DateTime RestoredAt) : DomainEventBase;
