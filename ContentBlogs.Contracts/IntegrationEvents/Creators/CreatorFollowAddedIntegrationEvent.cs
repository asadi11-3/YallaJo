using YallaJo.SharedKernel.Domain.Event;

namespace ContentBlogs.Contracts.IntegrationEvents.Creators;

public sealed record CreatorFollowAddedIntegrationEvent(
    Guid CreatorProfileId,
    Guid FollowerUserId,
    DateTime FollowedAtUtc) : IntegrationEventBase;
