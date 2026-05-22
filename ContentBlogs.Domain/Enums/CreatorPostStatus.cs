namespace ContentBlogs.Domain.Enums;

/// <summary>
/// Lifecycle status of a creator post.
/// Wave 8 – Multi-type Creator Posts.
/// </summary>
public enum CreatorPostStatus : byte
{
    /// <summary>Post is being composed; not yet visible.</summary>
    Draft = 0,

    /// <summary>Submitted by a Tier-0 creator; awaiting admin review.</summary>
    PendingReview = 1,

    /// <summary>Published and publicly visible.</summary>
    Published = 2,

    /// <summary>Rejected by an admin during moderation.</summary>
    Rejected = 3,

    /// <summary>Hidden by admin after publication (e.g. policy violation).</summary>
    Hidden = 4,

    /// <summary>Permanently removed by the creator.</summary>
    Removed = 5
}
