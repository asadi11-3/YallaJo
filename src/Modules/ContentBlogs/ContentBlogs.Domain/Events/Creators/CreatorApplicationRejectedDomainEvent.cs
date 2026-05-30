using YallaJo.SharedKernel.Domain.Event;

namespace ContentBlogs.Domain.Events.Creators;

/// <summary>Raised when an admin rejects a creator application.</summary>
public sealed record CreatorApplicationRejectedDomainEvent(
    Guid ApplicationId,
    Guid ApplicantUserId,
    Guid RejectedByAdminId,
    string Reason) : DomainEventBase;
