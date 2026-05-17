using YallaJo.SharedKernel.Domain.Event;

namespace Finance.Domain.Events;

public sealed record PaymentRefundedDomainEvent(Guid PaymentId, Guid UserId, decimal RefundedAmount, string Currency, string Reason) : DomainEventBase;
