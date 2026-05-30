using YallaJo.SharedKernel.Domain.Event;

namespace Messaging.Contracts.IntegrationEvents;

public sealed record SupportTicketResolvedIntegrationEvent(
    Guid TicketId,
    Guid ResolvedByUserId) : IntegrationEventBase;
