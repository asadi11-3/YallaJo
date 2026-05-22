using YallaJo.SharedKernel.Domain.Event;

namespace ContentBlogs.Domain.Events.Creators;

/// <summary>Raised when an admin rejects a submitted post.</summary>
public sealed record CreatorPostRejectedDomainEvent(
    Guid PostId,
    Guid CreatorProfileId,
    Guid RejectedByAdminId,
    string Reason) : DomainEventBase;
