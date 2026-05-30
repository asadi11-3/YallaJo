using Messaging.Domain.Enums;
using YallaJo.SharedKernel.Domain.Event;

namespace Messaging.Domain.Events;

public sealed record DeviceTokenRegisteredDomainEvent(
    Guid DeviceTokenId,
    Guid UserId,
    DevicePlatform Platform) : DomainEventBase;
