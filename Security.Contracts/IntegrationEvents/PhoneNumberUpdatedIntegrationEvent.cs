using YallaJo.SharedKernel.Domain.Event;

namespace Security.Contracts.IntegrationEvents;


public sealed record PhoneNumberUpdatedIntegrationEvent(
    Guid UserId,
    string PhoneNumber,
    bool IsPrimary) : IntegrationEventBase;
