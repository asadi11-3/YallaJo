using YallaJo.SharedKernel.Domain.Event;

namespace Finance.Contracts.IntegrationEvents;

public sealed record SubscriptionCancelledIntegrationEvent(
    Guid SubscriptionId,
    Guid UserId,
    Guid PlanId,
    string? Reason) : IntegrationEventBase;
