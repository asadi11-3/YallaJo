using YallaJo.SharedKernel.Domain.Event;

namespace ContentBlogs.Domain.Events.Creators;

/// <summary>Raised when a user follows a creator.</summary>
public sealed record CreatorFollowedDomainEvent(
    Guid CreatorProfileId,
    Guid FollowerUserId) : DomainEventBase;
