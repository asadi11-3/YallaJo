namespace ContentBlogs.Domain.Enums;

/// <summary>
/// Lifecycle state of a <see cref="Entities.Creators.CreatorProfile"/>.
/// Wave 7 – Content Creator Module.
/// </summary>
public enum CreatorProfileStatus : byte
{
    /// <summary>Profile is active and the creator can publish content.</summary>
    Active = 0,

    /// <summary>Profile has been suspended by an admin.</summary>
    Suspended = 1,

    /// <summary>Profile has been deactivated by the creator themselves.</summary>
    Deactivated = 2
}
