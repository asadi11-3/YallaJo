using YallaJo.SharedKernel.Domain.Event;

namespace Auth.Contracts.IntegrationEvents;

public sealed record ActivationTokenIssuedIntegrationEvent(
    Guid TokenId,
    Guid UserId,
    string DeliveryAddress,
    string PlainToken,
    string ActivationLink,
    DateTime ExpiresAt) : IntegrationEventBase;
