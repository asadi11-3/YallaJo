using YallaJo.SharedKernel.Domain.Event;

namespace ContentPlaces.Contracts.IntegrationEvents;

public sealed record BusinessMoreDocsRequestedIntegrationEvent(
    Guid BusinessId,
    Guid OwnerId,
    string Reason) : IntegrationEventBase;
