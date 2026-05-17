using YallaJo.SharedKernel.Domain.Event;

namespace Finance.Domain.Events;

public sealed record InvoiceOverdueDomainEvent(Guid InvoiceId, Guid UserId, string InvoiceNumber, DateOnly DueDate) : DomainEventBase;
