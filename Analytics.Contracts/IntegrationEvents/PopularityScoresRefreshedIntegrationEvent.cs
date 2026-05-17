using YallaJo.SharedKernel.Domain.Event;

namespace Analytics.Contracts.IntegrationEvents;

public sealed record PopularityScoresRefreshedIntegrationEvent(
    int EntitiesProcessed,
    DateTime RefreshedAt) : IntegrationEventBase;
