using YallaJo.SharedKernel.Domain.Event;

namespace ContentBlogs.Domain.Events.Creators;

/// <summary>Raised when a creator profile is created upon application approval.</summary>
public sealed record CreatorProfileCreatedDomainEvent(
    Guid ProfileId,
    Guid UserId,
    Guid ApplicationId) : DomainEventBase;
