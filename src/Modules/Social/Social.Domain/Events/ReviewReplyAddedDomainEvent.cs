using YallaJo.SharedKernel.Domain.Event;

namespace Social.Domain.Events;

/// <summary>Raised when a provider posts a reply to a review.</summary>
public sealed record ReviewReplyAddedDomainEvent(
    Guid ReviewId, Guid ReplyId, Guid ProviderUserId, DateTime AddedAt) : DomainEventBase;
