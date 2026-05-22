using YallaJo.SharedKernel.Domain.Event;

namespace ContentBlogs.Contracts.IntegrationEvents.Creators;

public sealed record CreatorPostSubmittedForReviewIntegrationEvent(
    Guid PostId,
    Guid CreatorProfileId,
    string Title,
    DateTime SubmittedAtUtc) : IntegrationEventBase;
