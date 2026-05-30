using YallaJo.SharedKernel.Domain.Event;

namespace ContentBlogs.Contracts.IntegrationEvents.Creators;

public sealed record CreatorApplicationSubmittedIntegrationEvent(
    Guid ApplicationId,
    Guid ApplicantUserId,
    DateTime SubmittedAtUtc) : IntegrationEventBase;
