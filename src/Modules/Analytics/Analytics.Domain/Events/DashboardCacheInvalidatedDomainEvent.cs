using YallaJo.SharedKernel.Domain.Event;

namespace Analytics.Domain.Events;

public sealed record DashboardCacheInvalidatedDomainEvent(string CacheKey, DateTime InvalidatedAt) : DomainEventBase;
