using YallaJo.SharedKernel.Domain.Event;

namespace Messaging.Contracts.IntegrationEvents;

public sealed record DeviceTokenRegisteredIntegrationEvent(
    Guid DeviceTokenId,
    Guid UserId,
    string Platform) : IntegrationEventBase;
