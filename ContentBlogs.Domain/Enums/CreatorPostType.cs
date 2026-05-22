namespace ContentBlogs.Domain.Enums;

/// <summary>
/// Discriminator for the type of creator post.
/// Wave 8 – Multi-type Creator Posts.
/// </summary>
public enum CreatorPostType : byte
{
    /// <summary>Embedded or externally-hosted video content.</summary>
    Video = 0,

    /// <summary>Photo-centric story with ordered image gallery.</summary>
    PhotoStory = 1,

    /// <summary>In-depth textual review of a place, tour, or business.</summary>
    LongReview = 2,

    /// <summary>Multi-stop travel itinerary with ordered waypoints.</summary>
    Itinerary = 3
}
