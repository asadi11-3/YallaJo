namespace Finance.Domain.Enums;

public enum DisputeStatus : byte
{
    Open = 0,
    UnderReview = 1,
    Resolved = 2,
    Escalated = 3,
    Closed = 4
}
