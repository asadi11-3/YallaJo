using YallaJo.SharedKernel.Domain.Event;

namespace Analytics.Contracts.IntegrationEvents;

public sealed record PopularityScoresRecalculatedIntegrationEvent(
    IReadOnlyList<string> EntityTypes,
    int TotalRecalculated,
    DateTime RecalculatedAt) : IntegrationEventBase;
