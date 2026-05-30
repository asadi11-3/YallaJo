using YallaJo.SharedKernel.Domain.Event;

namespace Social.Contracts.IntegrationEvents;

/// <summary>social.review.published.v1 — emitted when a review becomes publicly visible.</summary>
public sealed record ReviewPublishedIntegrationEvent(
    Guid ReviewId, Guid UserId,
    string TargetType, Guid TargetId,
    decimal Rating, bool IsVerifiedBooking, DateTime PublishedAt) : IntegrationEventBase;
