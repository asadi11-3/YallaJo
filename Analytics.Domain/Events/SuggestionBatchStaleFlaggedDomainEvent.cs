using YallaJo.SharedKernel.Domain.Event;

namespace Analytics.Domain.Events;

public sealed record SuggestionBatchStaleFlaggedDomainEvent(Guid BatchId, int SourceKind, Guid SourceId, int Context, DateTime FlaggedAt) : DomainEventBase;
