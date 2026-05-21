using YallaJo.SharedKernel.Domain.Event;

namespace Analytics.Contracts.IntegrationEvents;

public sealed record TrendingRefreshedIntegrationEvent(
    IReadOnlyList<string> EntityTypes,
    int TotalTrending,
    DateTime RefreshedAt) : IntegrationEventBase;
