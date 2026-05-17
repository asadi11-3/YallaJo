using YallaJo.SharedKernel.Domain.Event;

namespace Social.Domain.Events;

public sealed record ReviewDeletedDomainEvent(Guid ReviewId, Guid UserId, string Reason) : DomainEventBase;
