using YallaJo.SharedKernel.Domain.Event;

namespace Messaging.Contracts.IntegrationEvents;

public sealed record NotificationTemplateUpdatedIntegrationEvent(
    Guid TemplateId,
    string Code) : IntegrationEventBase;
