namespace Social.Domain.Enums;

/// <summary>
/// Lifecycle status for an <see cref="Entities.AccessibilityReview"/>.
/// MVP scope (Phase-3 WS-2 option b): simpler than <see cref="ReviewStatus"/> — no
/// AwaitingModeration / AutoHidden / profanity flow. Moderation queue deferred to Phase 3.5.
/// </summary>
public enum AccessibilityReviewStatus : byte
{
    Published      = 0,
    RemovedByAdmin = 1,
    DeletedByUser  = 2,
}
