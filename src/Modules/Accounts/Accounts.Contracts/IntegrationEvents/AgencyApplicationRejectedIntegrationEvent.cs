using YallaJo.SharedKernel.Domain.Event;

namespace Accounts.Contracts.IntegrationEvents;

public sealed record AgencyApplicationRejectedIntegrationEvent(
    Guid ApplicationId,
    Guid GuideUserId,
    Guid AgencyUserId,
    string Reason,
    DateTime RejectedAt) : IntegrationEventBase;
