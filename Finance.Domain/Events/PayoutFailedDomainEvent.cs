using YallaJo.SharedKernel.Domain.Event;

namespace Finance.Domain.Events;

public sealed record PayoutFailedDomainEvent(Guid PayoutId, Guid RecipientUserId, string Reason) : DomainEventBase;
