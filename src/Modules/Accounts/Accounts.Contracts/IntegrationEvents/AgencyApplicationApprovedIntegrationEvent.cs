using YallaJo.SharedKernel.Domain.Event;

namespace Accounts.Contracts.IntegrationEvents;

public sealed record AgencyApplicationApprovedIntegrationEvent(
    Guid ApplicationId,
    Guid GuideUserId,
    Guid AgencyUserId,
    DateTime ApprovedAt) : IntegrationEventBase;
