using YallaJo.SharedKernel.Domain.Event;

namespace ContentBlogs.Domain.Events.Creators;

/// <summary>Raised when a Tier-0 creator submits a post for admin review.</summary>
public sealed record CreatorPostSubmittedDomainEvent(
    Guid PostId,
    Guid CreatorProfileId,
    DateTime SubmittedAtUtc) : DomainEventBase;
