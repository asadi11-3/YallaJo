using YallaJo.SharedKernel.Domain.Event;

namespace Messaging.Contracts.IntegrationEvents;

public sealed record SupportTicketOpenedIntegrationEvent(
    Guid TicketId,
    Guid UserId,
    string Priority,
    string Subject) : IntegrationEventBase;
