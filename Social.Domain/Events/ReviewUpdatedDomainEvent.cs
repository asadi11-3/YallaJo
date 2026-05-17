using YallaJo.SharedKernel.Domain.Event;

namespace Social.Domain.Events;

public sealed record ReviewUpdatedDomainEvent(Guid ReviewId, Guid UserId, decimal OldRating, decimal NewRating) : DomainEventBase;
