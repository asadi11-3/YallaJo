using YallaJo.SharedKernel.Domain.Event;

namespace ContentBlogs.Domain.Events.Creators;

/// <summary>Raised when an admin approves a creator application.</summary>
public sealed record CreatorApplicationApprovedDomainEvent(
    Guid ApplicationId,
    Guid ApplicantUserId,
    Guid ApprovedByAdminId) : DomainEventBase;
