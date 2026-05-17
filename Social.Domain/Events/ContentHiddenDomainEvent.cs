using YallaJo.SharedKernel.Domain.Event;

namespace Social.Domain.Events;

public sealed record ContentHiddenDomainEvent(Guid LogId, string EntityType, Guid EntityId, Guid ModeratorUserId, string? Reason) : DomainEventBase;
