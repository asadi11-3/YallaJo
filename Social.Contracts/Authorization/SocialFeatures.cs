namespace Social.Contracts.Authorization;

/// <summary>Feature-name constants used to build Social module permissions.</summary>
public static class SocialFeatures
{
    /// <summary>Review CRUD — created by users, public visibility.</summary>
    public const string Review = nameof(Review);

    /// <summary>Provider replies to reviews.</summary>
    public const string ReviewReply = nameof(ReviewReply);

    /// <summary>User-owned favorites list (max 500 per user).</summary>
    public const string Favorite = nameof(Favorite);

    /// <summary>Abuse / content report submitted by users.</summary>
    public const string Report = nameof(Report);

    /// <summary>Append-only admin audit trail of moderation actions.</summary>
    public const string ContentModerationLog = nameof(ContentModerationLog);

    /// <summary>Admin operations on the flagged-content review queue.</summary>
    public const string AdminModerationQueue = nameof(AdminModerationQueue);

    /// <summary>User warning / temporary-ban moderation records.</summary>
    public const string UserModeration = nameof(UserModeration);
}
