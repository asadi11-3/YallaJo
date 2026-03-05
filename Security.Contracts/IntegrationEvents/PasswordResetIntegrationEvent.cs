using YallaJo.SharedKernel.Domain.Event;

namespace Security.Contracts.IntegrationEvents;


public sealed record PasswordResetIntegrationEvent(
    Guid UserId) : IntegrationEventBase;
