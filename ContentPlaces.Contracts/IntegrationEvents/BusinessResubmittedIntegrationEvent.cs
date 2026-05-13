using YallaJo.SharedKernel.Domain.Event;

namespace ContentPlaces.Contracts.IntegrationEvents;

public sealed record BusinessResubmittedIntegrationEvent(
    Guid BusinessId,
    Guid OwnerId) : IntegrationEventBase;
