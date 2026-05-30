namespace Social.Domain.Enums;

/// <summary>Reason a user is reporting content.</summary>
public enum ReportReason : byte
{
    Spam          = 0,
    Inappropriate = 1,
    Misleading    = 2,
    Harassment    = 3,
    FakeReview    = 4,
    Other         = 5,
}
