using YallaJo.SharedKernel.Domain.Event;

namespace Analytics.Domain.Events;

public sealed record AuditLogEntryRedactedDomainEvent(long EntryId, int Action, string EntityType, Guid EntityId, IReadOnlyList<string> RedactedFields) : DomainEventBase;
