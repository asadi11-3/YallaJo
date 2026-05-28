namespace ContentBlogs.Domain.Enums;

/// <summary>
/// Trust / privilege tier for a creator profile.
/// Higher tiers may unlock features such as auto-publish.
/// Wave 7 – Content Creator Module.
/// </summary>
public enum CreatorTrustTier : byte
{
    /// <summary>Newly approved creator; articles require admin review.</summary>
    New = 0,

    /// <summary>Creator with a track record; may receive lighter moderation.</summary>
    Trusted = 1,

    /// <summary>Top-tier creator; may auto-publish articles.</summary>
    Expert = 2
}
