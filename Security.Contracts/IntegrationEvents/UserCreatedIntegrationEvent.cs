using YallaJo.SharedKernel.Domain.Event;

namespace Security.Contracts.IntegrationEvents;


public sealed record UserCreatedIntegrationEvent(
    Guid UserId,
    string Email,
    string FirstName,
    string LastName) : IntegrationEventBase;
