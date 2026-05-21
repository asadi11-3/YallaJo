using YallaJo.SharedKernel.Domain.Event;

namespace Analytics.Domain.Events;

public sealed record TrendingRefreshedDomainEvent(int EntityType, int TopRankedCount, DateTime RefreshedAt) : DomainEventBase;
