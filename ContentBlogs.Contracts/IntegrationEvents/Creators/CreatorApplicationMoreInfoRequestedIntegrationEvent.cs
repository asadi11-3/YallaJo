using YallaJo.SharedKernel.Domain.Event;

namespace ContentBlogs.Contracts.IntegrationEvents.Creators;

public sealed record CreatorApplicationMoreInfoRequestedIntegrationEvent(
    Guid ApplicationId,
    Guid ApplicantUserId,
    Guid RequestedByAdminId,
    string AdminNote,
    DateTime RequestedAtUtc) : IntegrationEventBase;
