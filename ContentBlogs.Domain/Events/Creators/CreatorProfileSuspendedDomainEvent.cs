using YallaJo.SharedKernel.Domain.Event;

namespace ContentBlogs.Domain.Events.Creators;

/// <summary>Raised when an admin suspends a creator profile.</summary>
public sealed record CreatorProfileSuspendedDomainEvent(
    Guid ProfileId,
    Guid UserId,
    Guid SuspendedByAdminId,
    string Reason) : DomainEventBase;
