using YallaJo.SharedKernel.Domain.Event;

namespace ContentBlogs.Domain.Events.Creators;

/// <summary>Raised when a creator permanently removes their own post.</summary>
public sealed record CreatorPostRemovedDomainEvent(
    Guid PostId,
    Guid CreatorProfileId) : DomainEventBase;
