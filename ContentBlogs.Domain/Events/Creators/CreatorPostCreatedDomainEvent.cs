using ContentBlogs.Domain.Enums;
using YallaJo.SharedKernel.Domain.Event;

namespace ContentBlogs.Domain.Events.Creators;

/// <summary>Raised when a new creator post is created (Draft status).</summary>
public sealed record CreatorPostCreatedDomainEvent(
    Guid PostId,
    Guid CreatorProfileId,
    CreatorPostType PostType) : DomainEventBase;
