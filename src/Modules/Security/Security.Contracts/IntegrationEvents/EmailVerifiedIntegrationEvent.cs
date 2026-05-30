using YallaJo.SharedKernel.Domain.Event;

namespace Security.Contracts.IntegrationEvents;


public sealed record EmailVerifiedIntegrationEvent(
    Guid UserId,
    Guid EmailId,
    string EmailAddress) : IntegrationEventBase;
