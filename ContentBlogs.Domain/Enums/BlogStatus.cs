namespace ContentBlogs.Domain.Enums;

public enum BlogStatus : byte
{
    Draft = 0,
    Published = 1,
    Archived = 2,

    /// <summary>
    /// Creator-authored article awaiting admin review before publishing.
    /// </summary>
    PendingCreatorReview = 99,

    /// <summary>
    /// Hidden by admin — not visible to public but still editable by the creator.
    /// </summary>
    Hidden = 100,
}
