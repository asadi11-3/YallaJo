using YallaJo.SharedKernel.Domain.Event;

namespace Analytics.Domain.Events;

public sealed record SuggestionBatchRefreshedDomainEvent(Guid BatchId, int SourceKind, Guid SourceId, int Context, string AlgorithmVersion, int ItemCount, DateTime RefreshedAt) : DomainEventBase;
