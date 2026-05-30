using YallaJo.SharedKernel.Domain.Event;

namespace ContentBlogs.Domain.Events.Creators;

/// <summary>Raised when a creator invitation expires (batch cleanup).</summary>
public sealed record CreatorInvitationExpiredDomainEvent(
    Guid InvitationId) : DomainEventBase;
