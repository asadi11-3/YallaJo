namespace ContentTours.Domain.Enums;

/// <summary>
/// Tour lifecycle status.
///
/// Lifecycle (post Task-1):
///   Draft  ──Submit──►  Pending  ──Approve──►  Approved  ──Suspend──►  Suspended
///                          │                       │                     │
///                          └──Reject──► Rejected   └──Archive──► Archived
///                                          │
///                                          └──Update (auto)──► Draft
///
/// Task-1 byte renumber: byte values now follow the canonical lifecycle order
/// (Draft → Pending → Approved → Rejected → Suspended → Archived). A data backfill
/// migration is required to remap existing rows from the legacy byte layout.
/// </summary>
public enum TourStatus : byte
{
    Draft     = 0,
    Pending   = 1,
    Approved  = 2,
    Rejected  = 3,
    Suspended = 4,
    Archived  = 5,
}
