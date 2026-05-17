using YallaJo.SharedKernel.Domain.Event;

namespace Finance.Domain.Events;

public sealed record DisputeOpenedDomainEvent(Guid DisputeId, Guid PaymentId, Guid UserId, string Reason) : DomainEventBase;
