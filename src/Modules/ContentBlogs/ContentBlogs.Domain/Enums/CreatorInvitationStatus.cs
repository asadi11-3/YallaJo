namespace ContentBlogs.Domain.Enums;

/// <summary>
/// Lifecycle state of a <see cref="Entities.Creators.CreatorInvitation"/>.
/// Wave 7 – Content Creator Module.
/// </summary>
public enum CreatorInvitationStatus : byte
{
    /// <summary>Invitation sent and awaiting action.</summary>
    Pending = 0,

    /// <summary>Recipient accepted and redeemed the invitation.</summary>
    Redeemed = 1,

    /// <summary>Invitation expired after the 14-day window.</summary>
    Expired = 2,

    /// <summary>Invitation was revoked by an admin before redemption.</summary>
    Revoked = 3
}
