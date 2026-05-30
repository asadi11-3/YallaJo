using YallaJo.SharedKernel.Domain.Event;

namespace Auth.Contracts.IntegrationEvents;

/// <summary>Emitted when a new user account is successfully created.</summary>
public sealed record UserRegisteredIntegrationEvent(
    Guid UserId,
    string Email,
    string? FullName,
    string LanguageCode,
    DateTime RegisteredAt) : IntegrationEventBase;
