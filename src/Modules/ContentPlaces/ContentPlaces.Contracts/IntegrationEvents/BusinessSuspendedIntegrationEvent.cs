using YallaJo.SharedKernel.Domain.Event;

namespace ContentPlaces.Contracts.IntegrationEvents;

public sealed record BusinessSuspendedIntegrationEvent(
    Guid BusinessId,
    Guid OwnerId,
    string Reason) : IntegrationEventBase;
