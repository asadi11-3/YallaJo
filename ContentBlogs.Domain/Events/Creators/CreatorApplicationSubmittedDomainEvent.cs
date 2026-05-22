using YallaJo.SharedKernel.Domain.Event;

namespace ContentBlogs.Domain.Events.Creators;

/// <summary>Raised when a creator application is submitted for review.</summary>
public sealed record CreatorApplicationSubmittedDomainEvent(
    Guid ApplicationId,
    Guid ApplicantUserId) : DomainEventBase;
