using YallaJo.SharedKernel.Domain.Event;

namespace Finance.Contracts.IntegrationEvents;

public sealed record InvoiceIssuedIntegrationEvent(
    Guid InvoiceId,
    Guid UserId,
    string InvoiceNumber,
    decimal TotalAmount,
    string Currency,
    DateOnly DueDate) : IntegrationEventBase;
