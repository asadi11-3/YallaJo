using YallaJo.SharedKernel.Domain.Event;

namespace ContentBlogs.Domain.Events.Creators;

/// <summary>Raised when an admin requests more information on a creator application.</summary>
public sealed record CreatorApplicationMoreInfoRequestedDomainEvent(
    Guid ApplicationId,
    Guid ApplicantUserId,
    Guid RequestedByAdminId,
    string AdminNote) : DomainEventBase;
