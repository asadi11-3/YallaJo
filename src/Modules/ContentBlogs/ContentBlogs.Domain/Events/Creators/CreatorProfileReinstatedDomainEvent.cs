using YallaJo.SharedKernel.Domain.Event;

namespace ContentBlogs.Domain.Events.Creators;

/// <summary>Raised when an admin reinstates a previously suspended creator profile.</summary>
public sealed record CreatorProfileReinstatedDomainEvent(
    Guid ProfileId,
    Guid UserId,
    Guid ReinstatedByAdminId) : DomainEventBase;
