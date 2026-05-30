namespace ContentTours.Domain.Enums;

/// <summary>
/// Centralises status-semantics logic so a future status workflow change is a single-file edit
/// instead of a hunt across handlers.
///
/// PW-1 (2026-05) introduced the <c>Pending</c> and <c>Rejected</c> states and renamed
/// <c>Published</c> → <c>Approved</c>. These extension methods reflect the new semantics.
/// </summary>
public static class TourStatusExtensions
{
    /// <summary>
    /// True for statuses that allow public visibility and admin curation (e.g. ToggleFeatured).
    /// Maps to <see cref="TourStatus.Approved"/>.
    /// </summary>
    public static bool IsApproved(this TourStatus status)
        => status == TourStatus.Approved;

    /// <summary>
    /// True for statuses where the Adult-tier guard is enforced (deleting / deactivating /
    /// renaming-away-from-Adult the last active Adult tier is blocked).
    /// Maps to <see cref="TourStatus.Pending"/> or <see cref="TourStatus.Approved"/>.
    /// </summary>
    public static bool RequiresAdultTier(this TourStatus status)
        => status is TourStatus.Pending or TourStatus.Approved;

    /// <summary>
    /// True when the tour is still editable by the provider (drafting or revising-after-rejection).
    /// Maps to <see cref="TourStatus.Draft"/> or <see cref="TourStatus.Rejected"/>.
    /// </summary>
    public static bool IsEditable(this TourStatus status)
        => status is TourStatus.Draft or TourStatus.Rejected;

    /// <summary>
    /// True for statuses that can transition to <see cref="TourStatus.Pending"/> via <c>Tour.Submit()</c>.
    /// Same set as <see cref="IsEditable"/> by current rules but kept separate so the two semantics
    /// can diverge (e.g. if Suspended-tour resubmission is added later).
    /// </summary>
    public static bool IsSubmittable(this TourStatus status)
        => status is TourStatus.Draft or TourStatus.Rejected;
}
