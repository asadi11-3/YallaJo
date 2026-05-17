using YallaJo.SharedKernel.Domain.Event;

namespace Messaging.Contracts.IntegrationEvents;

public sealed record NotificationFailedIntegrationEvent(
    Guid NotificationId,
    Guid UserId,
    string Channel,
    string Error) : IntegrationEventBase;
