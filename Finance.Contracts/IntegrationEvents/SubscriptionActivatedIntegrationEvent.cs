using YallaJo.SharedKernel.Domain.Event;

namespace Finance.Contracts.IntegrationEvents;

public sealed record SubscriptionActivatedIntegrationEvent(
    Guid SubscriptionId,
    Guid UserId,
    Guid PlanId) : IntegrationEventBase;
