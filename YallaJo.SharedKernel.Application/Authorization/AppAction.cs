namespace YallaJo.SharedKernel.Application.Authorization;

/// <summary>
/// Framework-agnostic verb constants used in permission declarations.
/// Every module uses the same verbs — therefore cross-cutting, lives in SharedKernel.
/// </summary>
public static class AppAction
{
    public const string Read       = nameof(Read);
    public const string Create     = nameof(Create);
    public const string Update     = nameof(Update);
    public const string Delete     = nameof(Delete);
    public const string UpdateSelf = nameof(UpdateSelf);
    public const string UpdateAny  = nameof(UpdateAny);
    public const string DeleteAny  = nameof(DeleteAny);
    public const string SoftDelete = nameof(SoftDelete);
    public const string Manage     = nameof(Manage);
    public const string Approve    = nameof(Approve);
    public const string Submit     = nameof(Submit);
    public const string Reject     = nameof(Reject);
    public const string Suspend    = nameof(Suspend);
    public const string Reinstate  = nameof(Reinstate);
    public const string Replay     = nameof(Replay);
    public const string ReadOwn    = nameof(ReadOwn);
    public const string ReadAny    = nameof(ReadAny);
    public const string Feature    = nameof(Feature);
    public const string Refresh    = nameof(Refresh);   // for batch/cache refresh admin endpoints (e.g. recommendations)
    public const string Record     = nameof(Record);    // for write-only ingestion endpoints (e.g. interaction events)

    // ── Extended verbs (Wave 5–6 modules) ──────────────────────────────────

    /// <summary>Messaging: assign support ticket.</summary>
    public const string Assign   = nameof(Assign);

    /// <summary>Social: ban user (moderation).</summary>
    public const string Ban      = nameof(Ban);

    /// <summary>Booking: cancel booking.</summary>
    public const string Cancel   = nameof(Cancel);

    /// <summary>Social/Messaging: close report or ticket.</summary>
    public const string Close    = nameof(Close);

    /// <summary>Booking: mark booking complete.</summary>
    public const string Complete = nameof(Complete);

    /// <summary>Booking: confirm booking.</summary>
    public const string Confirm  = nameof(Confirm);

    /// <summary>Finance/Analytics: download invoice/export.</summary>
    public const string Download = nameof(Download);

    /// <summary>Analytics/Finance: admin data export.</summary>
    public const string Export   = nameof(Export);

    /// <summary>Analytics: PII redaction.</summary>
    public const string Redact   = nameof(Redact);

    /// <summary>Finance: issue refund.</summary>
    public const string Refund   = nameof(Refund);

    /// <summary>Social/Moderation: remove content (admin action).</summary>
    public const string Remove   = nameof(Remove);

    /// <summary>Messaging/Finance: resolve ticket or dispute.</summary>
    public const string Resolve  = nameof(Resolve);

    /// <summary>Booking/Finance: trigger background batch.</summary>
    public const string Trigger  = nameof(Trigger);

    /// <summary>Auth-Cleanup/Booking: verify document/identity.</summary>
    public const string Verify   = nameof(Verify);

    /// <summary>Social: warn user (moderation).</summary>
    public const string Warn     = nameof(Warn);

    /// <summary>Accounts: register as a provider (initial draft creation).</summary>
    public const string Register = nameof(Register);

    /// <summary>Accounts/Admin: request additional documents from provider applicant.</summary>
    public const string RequestDocs = nameof(RequestDocs);

    // ── Extended verbs (Wave 7 – Content Creators) ─────────────────────────

    /// <summary>ContentBlogs: invite a user to become a content creator.</summary>
    public const string Invite = nameof(Invite);

    /// <summary>ContentBlogs: redeem a creator invitation via token.</summary>
    public const string RedeemInvitation = nameof(RedeemInvitation);

    /// <summary>ContentBlogs: admin requests more info from creator applicant.</summary>
    public const string RequestMoreInfo = nameof(RequestMoreInfo);

    /// <summary>ContentBlogs: follow a content creator.</summary>
    public const string Follow = nameof(Follow);

    /// <summary>ContentBlogs: unfollow a content creator.</summary>
    public const string Unfollow = nameof(Unfollow);

    // ── Wave 8 – Creator Posts & Tier Management ─────────────────────────

    /// <summary>ContentBlogs: admin promotes a creator to a higher trust tier.</summary>
    public const string PromoteTier = nameof(PromoteTier);

    /// <summary>ContentBlogs: admin demotes a creator to a lower trust tier.</summary>
    public const string DemoteTier = nameof(DemoteTier);

    /// <summary>ContentTours: provider archives own tour.</summary>
    public const string Archive = nameof(Archive);

    /// <summary>ContentBlogs: admin hides a creator post for policy violations.</summary>
    public const string HidePost = nameof(HidePost);

    /// <summary>ContentBlogs: admin unhides a hidden creator post.</summary>
    public const string UnhidePost = nameof(UnhidePost);
}
