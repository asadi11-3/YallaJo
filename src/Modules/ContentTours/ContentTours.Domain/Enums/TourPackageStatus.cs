namespace ContentTours.Domain.Enums;

/// <summary>
/// Approval lifecycle for a TourPackage.
/// Draft → Submitted → Approved | Rejected.
/// Rejected packages can be edited back to Draft and resubmitted.
/// Mirrors TourProposalStatus / Tour's own approval flow.
/// </summary>
public enum TourPackageStatus : byte
{
    Draft = 0,
    Submitted = 1,
    Approved = 2,
    Rejected = 3,
}
