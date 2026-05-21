using YallaJo.SharedKernel.Domain.Event;

namespace Social.Contracts.IntegrationEvents;

/// <summary>social.review.deleted.v1 — emitted when a review is removed from the platform.</summary>
public sealed record ReviewDeletedIntegrationEvent(
    Guid ReviewId, Guid UserId, string TargetType, Guid TargetId,
    DateTime DeletedAt, string DeletionSource) : IntegrationEventBase;
