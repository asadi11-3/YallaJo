using YallaJo.SharedKernel.Domain.Event;

namespace Finance.Domain.Events;

public sealed record PaymentInitiatedDomainEvent(Guid PaymentId, Guid UserId, Guid? BookingId, decimal Amount, string Currency) : DomainEventBase;
