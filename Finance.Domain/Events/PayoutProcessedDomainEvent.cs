using YallaJo.SharedKernel.Domain.Event;

namespace Finance.Domain.Events;

public sealed record PayoutProcessedDomainEvent(Guid PayoutId, Guid RecipientUserId, string TransactionId) : DomainEventBase;
