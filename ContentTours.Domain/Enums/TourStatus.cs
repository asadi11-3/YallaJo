namespace ContentTours.Domain.Enums;

/// <summary>
/// Tour lifecycle status.
///
/// Lifecycle (post PW-1):
///   Draft  ──Submit──►  Pending  ──Approve──►  Approved  ──Suspend──►  Suspended
///                          │                       │                     │
///                          └──Reject──► Rejected   └──Archive──► Archived
///                                          │
///                                          └──Submit──► Pending
///
/// Byte values: <c>Approved = 1</c> is preserved from the legacy <c>Published = 1</c> so existing
/// rows do not need a data migration. New states <c>Pending</c> and <c>Rejected</c> use the next
/// available bytes.
/// </summary>
public enum TourStatus : byte
{
    Draft     = 0,
    Approved  = 1,   // was: Published — byte preserved for backward compatibility
    Archived  = 2,
    Suspended = 3,
    Pending   = 4,   // PW-1: new — submitted, awaiting admin review
    Rejected  = 5,   // PW-1: new — admin rejected; provider may revise and resubmit
}
