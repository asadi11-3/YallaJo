using YallaJo.SharedKernel.Domain.Event;

namespace ContentPlaces.Contracts.IntegrationEvents;

public sealed record BusinessApprovedIntegrationEvent(
    Guid BusinessId,
    Guid OwnerId) : IntegrationEventBase;
