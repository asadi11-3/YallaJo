using YallaJo.SharedKernel.Domain.Event;

namespace ContentBlogs.Domain.Events.Creators;

/// <summary>Raised when a creator post transitions to Published.</summary>
public sealed record CreatorPostPublishedDomainEvent(
    Guid PostId,
    Guid CreatorProfileId,
    bool IsPreModerated,
    DateTime PublishedAtUtc) : DomainEventBase;
