using YallaJo.SharedKernel.Domain.Event;

namespace Social.Domain.Events;

public sealed record ContentRestoredDomainEvent(Guid LogId, string EntityType, Guid EntityId, Guid ModeratorUserId) : DomainEventBase;
