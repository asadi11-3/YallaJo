using YallaJo.SharedKernel.Domain.Event;

namespace Messaging.Contracts.IntegrationEvents;

public sealed record NotificationDeliveredIntegrationEvent(
    Guid NotificationId,
    Guid UserId,
    string Channel,
    DateTime SentAt) : IntegrationEventBase;
