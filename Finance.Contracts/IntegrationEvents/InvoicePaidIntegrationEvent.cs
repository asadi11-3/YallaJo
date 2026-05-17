using YallaJo.SharedKernel.Domain.Event;

namespace Finance.Contracts.IntegrationEvents;

public sealed record InvoicePaidIntegrationEvent(
    Guid InvoiceId,
    Guid UserId,
    string InvoiceNumber) : IntegrationEventBase;
