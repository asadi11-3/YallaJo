using YallaJo.SharedKernel.Domain.Event;

namespace ContentBlogs.Contracts.IntegrationEvents.Creators;

public sealed record CreatorApplicationRejectedIntegrationEvent(
    Guid ApplicationId,
    Guid ApplicantUserId,
    Guid RejectedByAdminId,
    string Reason,
    DateTime RejectedAtUtc) : IntegrationEventBase;
