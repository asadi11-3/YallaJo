using YallaJo.SharedKernel.Domain.Event;

namespace Finance.Domain.Events;

public sealed record InvoiceIssuedDomainEvent(Guid InvoiceId, Guid UserId, string InvoiceNumber, decimal TotalAmount, string Currency) : DomainEventBase;
