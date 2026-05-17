using YallaJo.SharedKernel.Domain.Event;

namespace Finance.Domain.Events;

public sealed record SubscriptionCancelledDomainEvent(Guid SubscriptionId, Guid UserId, Guid PlanId, string? Reason) : DomainEventBase;
