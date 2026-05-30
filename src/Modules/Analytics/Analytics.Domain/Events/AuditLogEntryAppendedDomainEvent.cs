using YallaJo.SharedKernel.Domain.Event;

namespace Analytics.Domain.Events;

public sealed record AuditLogEntryAppendedDomainEvent(long EntryId, Guid? UserId, int Action, string EntityType, Guid EntityId, DateTime OccurredAt) : DomainEventBase;
