namespace ContentBlogs.Domain.Enums;

public enum BlogStatus : byte
{
    Draft = 0,

    /// <summary>
    /// Tier-0 creator-authored article awaiting admin review before publishing.
    /// </summary>
    PendingReview = 1,

    Published = 2,

    /// <summary>
    /// Admin rejected the blog (with a reason). Creator can revise and resubmit.
    /// </summary>
    Rejected = 3,

    Archived = 4,

    /// <summary>
    /// Hidden by admin — not visible to public but still accessible by the creator.
    /// </summary>
    Hidden = 5,

    /// <summary>
    /// Removed by admin (moderation). Permanently unavailable.
    /// </summary>
    Removed = 6,
}
