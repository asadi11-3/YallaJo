namespace Social.Domain.Enums;

/// <summary>Lifecycle status of an abuse report.</summary>
public enum ReportStatus : byte
{
    Open        = 0,
    UnderReview = 1,
    Resolved    = 2,
    Dismissed   = 3,
}
