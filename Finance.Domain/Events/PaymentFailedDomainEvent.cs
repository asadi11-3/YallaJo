using YallaJo.SharedKernel.Domain.Event;

namespace Finance.Domain.Events;

public sealed record PaymentFailedDomainEvent(Guid PaymentId, Guid UserId, string Reason) : DomainEventBase;
