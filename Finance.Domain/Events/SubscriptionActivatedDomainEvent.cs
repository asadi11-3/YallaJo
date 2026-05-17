using YallaJo.SharedKernel.Domain.Event;

namespace Finance.Domain.Events;

public sealed record SubscriptionActivatedDomainEvent(Guid SubscriptionId, Guid UserId, Guid PlanId) : DomainEventBase;
