namespace ContentBlogs.Domain.Enums;

/// <summary>
/// Delivery channel for a creator invitation.
/// Wave 7 – Content Creator Module.
/// </summary>
public enum CreatorInvitationKind : byte
{
    /// <summary>Invitation sent via email to an external address.</summary>
    Email = 0,

    /// <summary>Invitation sent in-app to an existing platform user.</summary>
    InApp = 1
}
