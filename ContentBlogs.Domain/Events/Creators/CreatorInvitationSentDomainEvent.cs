using ContentBlogs.Domain.Enums;
using YallaJo.SharedKernel.Domain.Event;

namespace ContentBlogs.Domain.Events.Creators;

/// <summary>Raised when an admin sends a creator invitation.</summary>
public sealed record CreatorInvitationSentDomainEvent(
    Guid InvitationId,
    CreatorInvitationKind Kind,
    string? Email,
    Guid? InvitedUserId,
    Guid SentByAdminId) : DomainEventBase;
