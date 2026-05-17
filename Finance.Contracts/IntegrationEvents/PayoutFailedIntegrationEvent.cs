using YallaJo.SharedKernel.Domain.Event;

namespace Finance.Contracts.IntegrationEvents;

public sealed record PayoutFailedIntegrationEvent(
    Guid PayoutId,
    Guid RecipientUserId,
    string Reason) : IntegrationEventBase;
