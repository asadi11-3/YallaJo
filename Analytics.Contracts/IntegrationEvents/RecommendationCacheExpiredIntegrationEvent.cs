using YallaJo.SharedKernel.Domain.Event;

namespace Analytics.Contracts.IntegrationEvents;

public sealed record RecommendationCacheExpiredIntegrationEvent(
    Guid UserId,
    DateTime ExpiredAt) : IntegrationEventBase;
