using YallaJo.SharedKernel.Domain.Event;

namespace Finance.Domain.Events;

public sealed record PayoutCreatedDomainEvent(Guid PayoutId, Guid RecipientUserId, decimal TotalAmount, string Currency) : DomainEventBase;
