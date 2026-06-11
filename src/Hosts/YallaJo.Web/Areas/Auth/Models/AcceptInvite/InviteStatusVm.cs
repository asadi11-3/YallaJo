namespace YallaJo.Web.Areas.Auth.Models.AcceptInvite;

public enum InviteStatusKind
{
    /// <summary>The accept-invite link is missing its email/token parameters or is malformed.</summary>
    InvalidLink,

    /// <summary>The invitation token has expired; a new invite must be issued by an administrator.</summary>
    Expired,
}

/// <summary>
/// Model for the consolidated AcceptInvite/Status view (replaces the former
/// Expired.cshtml + InvalidLink.cshtml pair, which were ~94% identical markup).
/// </summary>
public sealed class InviteStatusVm
{
    public InviteStatusKind Kind { get; init; }

    /// <summary>Invited email, shown on the Expired variant only (display-only — resend is admin-only).</summary>
    public string? Email { get; init; }
}
