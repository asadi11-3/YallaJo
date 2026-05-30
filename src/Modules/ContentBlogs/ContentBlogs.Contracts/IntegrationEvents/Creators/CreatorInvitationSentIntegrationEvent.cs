using YallaJo.SharedKernel.Domain.Event;

namespace ContentBlogs.Contracts.IntegrationEvents.Creators;

public sealed record CreatorInvitationSentIntegrationEvent(
    Guid InvitationId,
    string Kind,
    string? Email,
    Guid? InvitedUserId,
    Guid SentByAdminId,
    DateTime ExpiresAtUtc) : IntegrationEventBase;
