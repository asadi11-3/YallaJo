using YallaJo.SharedKernel.Domain.Event;

namespace ContentBlogs.Domain.Events.Creators;

/// <summary>Raised when a post is unfeatured (manually or by expiration).</summary>
public sealed record CreatorPostUnfeaturedDomainEvent(
    Guid PostId,
    Guid CreatorProfileId) : DomainEventBase;
