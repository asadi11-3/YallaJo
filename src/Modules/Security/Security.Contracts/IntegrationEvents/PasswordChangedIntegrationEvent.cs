
using YallaJo.SharedKernel.Domain.Event;

namespace Security.Contracts.IntegrationEvents;


public sealed record PasswordChangedIntegrationEvent(
    Guid UserId) : IntegrationEventBase;
