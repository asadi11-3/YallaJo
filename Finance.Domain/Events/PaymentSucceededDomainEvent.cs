using YallaJo.SharedKernel.Domain.Event;

namespace Finance.Domain.Events;

public sealed record PaymentSucceededDomainEvent(Guid PaymentId, Guid UserId, Guid? BookingId, decimal Amount, string Currency, string TransactionId) : DomainEventBase;
