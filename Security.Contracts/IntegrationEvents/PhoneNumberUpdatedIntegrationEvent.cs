using YallaJo.SharedKernel.Domain.Event;

namespace Security.Contracts.IntegrationEvents;

/// <summary>
/// Published when a user's phone number is created or updated in the Security module.
/// Consumed by other modules that need to react to phone changes.
/// </summary>
public sealed record PhoneNumberUpdatedIntegrationEvent(
    Guid UserId,
    string PhoneNumber,
    bool IsPrimary) : IntegrationEventBase;
