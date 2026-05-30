using YallaJo.SharedKernel.Domain.Event;

namespace ContentBlogs.Domain.Events;

/// <summary>
/// Raised when a creator-authored article is submitted for admin review.
/// </summary>
public sealed record BlogSubmittedForCreatorReviewDomainEvent(
    Guid BlogId,
    Guid CreatorProfileId,
    DateTime SubmittedAtUtc) : DomainEventBase;
