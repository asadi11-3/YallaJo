using YallaJo.SharedKernel.Domain.Event;

namespace ContentBlogs.Contracts.IntegrationEvents.Creators;

public sealed record CreatorApplicationApprovedIntegrationEvent(
    Guid ApplicationId,
    Guid ApplicantUserId,
    Guid ApprovedByAdminId,
    DateTime ApprovedAtUtc) : IntegrationEventBase;
