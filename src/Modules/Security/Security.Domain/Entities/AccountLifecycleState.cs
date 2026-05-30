namespace Security.Domain.Entities;

/// <summary>
/// Explicit lifecycle of a <see cref="User"/> account in the admin-provisioned
/// onboarding model. Replaces the implicit <c>(IsActive, Email.IsVerified)</c>
/// pair as the source of truth for "what state is this account in?".
/// <para>
/// Phase 2A + 2B — both representations coexist: <see cref="User.IsActive"/>
/// is derived from <c>LifecycleState == Active</c> and persisted as a normal
/// column for backward compatibility with existing queries/projections.
/// Removal of the shadow column is deferred to Phase 4, after every read-side
/// query and admin UI has migrated to branching on
/// <see cref="AccountLifecycleState"/> directly.
/// </para>
/// <para>
/// Allowed transitions (enforced by <c>User.TransitionTo</c>):
/// <code>
///   Provisioned          → PendingActivation | Active (legacy)        | Archived
///   PendingActivation    → Active            | Provisioned (revoke)   | Archived
///   Active               → Suspended         | PendingPasswordReset   | PendingActivation (reassign) | Archived
///   Suspended            → Active            | PendingActivation (reassign)                          | Archived
///   PendingPasswordReset → Active            | PendingActivation (reassign)                          | Archived
///   Archived             → ∅ (terminal)
/// </code>
/// Self-transitions are permitted and are no-ops (idempotent).
/// <br/>
/// Phase 3C: the three <c>… → PendingActivation</c> edges tagged
/// <c>(reassign)</c> are opened ONLY for the admin reassignment flow
/// (<c>User.ReassignToPendingActivation</c>). No other caller invokes
/// these transitions.
/// </para>
/// </summary>
public enum AccountLifecycleState
{
    /// <summary>
    /// Admin created the identity. No password, no activation token sent yet,
    /// account cannot log in. Default value used by the EF migration's
    /// backfill path for <c>IsActive=false</c> users with no outstanding
    /// invite token.
    /// </summary>
    Provisioned = 0,

    /// <summary>
    /// Admin has issued an activation token (and presumably sent the email).
    /// Account still cannot log in; user must complete activation to set a
    /// password and verify the primary email.
    /// </summary>
    PendingActivation = 1,

    /// <summary>
    /// Fully onboarded — has a password, verified primary email, and is
    /// permitted to log in. The ONLY state in which login may succeed.
    /// </summary>
    Active = 2,

    /// <summary>
    /// Admin temporarily disabled the account. Sessions and refresh tokens
    /// are revoked at suspension time. Recovery (forgot-password) is also
    /// blocked. Reversible via <c>User.Reactivate()</c>.
    /// </summary>
    Suspended = 3,

    /// <summary>
    /// Admin forced a password reset (Phase 2C+). Account login is blocked
    /// until the user completes the reset, even though credentials still
    /// technically validate — closes the window where a stolen password
    /// could be used between admin's "reset" click and the user's email
    /// arrival. Reversible to <c>Active</c> on completion.
    /// </summary>
    PendingPasswordReset = 4,

    /// <summary>
    /// Terminal state. Account cannot log in, cannot recover, cannot be
    /// reassigned, cannot be reactivated. History preserved for audit and
    /// FK continuity across modules.
    /// </summary>
    Archived = 5,
}
