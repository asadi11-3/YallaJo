namespace ContentBlogs.Domain.Enums;

/// <summary>
/// How the creator application was initiated.
/// Wave 7 – Content Creator Module.
/// </summary>
public enum CreatorApplicationSource : byte
{
    /// <summary>User self-applied through the public form.</summary>
    SelfApplied = 0,

    /// <summary>User applied after receiving an invitation.</summary>
    Invitation = 1
}
