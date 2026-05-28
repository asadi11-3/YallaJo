using YallaJo.SharedKernel.Domain.Event;

namespace ContentBlogs.Domain.Events.Creators;

/// <summary>Raised when a creator invitation is redeemed by the recipient.</summary>
public sealed record CreatorInvitationRedeemedDomainEvent(
    Guid InvitationId,
    Guid RedeemedByUserId) : DomainEventBase;
