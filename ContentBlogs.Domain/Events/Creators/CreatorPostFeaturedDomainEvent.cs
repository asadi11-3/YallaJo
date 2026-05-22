using YallaJo.SharedKernel.Domain.Event;

namespace ContentBlogs.Domain.Events.Creators;

/// <summary>Raised when an admin features a published post.</summary>
public sealed record CreatorPostFeaturedDomainEvent(
    Guid PostId,
    Guid CreatorProfileId,
    Guid FeaturedByAdminId,
    DateTime? FeaturedUntilUtc) : DomainEventBase;
