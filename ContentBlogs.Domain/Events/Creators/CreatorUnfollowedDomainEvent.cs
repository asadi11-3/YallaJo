using YallaJo.SharedKernel.Domain.Event;

namespace ContentBlogs.Domain.Events.Creators;

/// <summary>Raised when a user unfollows a creator.</summary>
public sealed record CreatorUnfollowedDomainEvent(
    Guid CreatorProfileId,
    Guid FollowerUserId) : DomainEventBase;
