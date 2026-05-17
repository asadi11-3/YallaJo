using YallaJo.SharedKernel.Domain.Event;

namespace Social.Domain.Events;

public sealed record ReviewMarkedHelpfulDomainEvent(Guid ReviewId, Guid UserId, int HelpfulCount) : DomainEventBase;
