namespace Social.Domain.Enums;

/// <summary>Review lifecycle status.</summary>
public enum ReviewStatus : byte
{
    Published          = 0,
    AwaitingModeration = 1,
    AutoHidden         = 2,
    RemovedByAdmin     = 3,
    DeletedByUser      = 4,
}
