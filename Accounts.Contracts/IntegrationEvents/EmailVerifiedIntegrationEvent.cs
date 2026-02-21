using YallaJo.SharedKernel.Domain.Event;

namespace Accounts.Contracts.IntegrationEvents;

/// <summary>
/// Published when a user verifies their email address in the Accounts module.
/// Consumed by any module that needs to react to email verification (e.g. Auth).
/// </summary>
public sealed record EmailVerifiedIntegrationEvent(
    Guid UserId,
    Guid EmailId,
    string EmailAddress) : IntegrationEventBase;
