using YallaJo.SharedKernel.Domain.Event;

namespace Finance.Domain.Events;

public sealed record InvoicePaidDomainEvent(Guid InvoiceId, Guid UserId, string InvoiceNumber) : DomainEventBase;
