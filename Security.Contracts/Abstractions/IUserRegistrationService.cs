using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Security.Contracts.Abstractions;

/// <summary>
/// Security-owned capability exposed to other modules (e.g. Auth) for creating
/// and finalizing an identity/user. Keeps the <c>User</c> aggregate, password
/// hashing, email-verification and activation transitions strictly inside the
/// Security module — callers only pass plain data and receive results.
/// </summary>
public interface IUserRegistrationService
{
    /// <summary>
    /// Self-service registration: user supplies their own password. The user
    /// is created with an unverified primary email and remains inactive until
    /// email verification is completed.
    /// </summary>
    Task<Result<Guid>> RegisterAsync(
        UserRegistrationRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Phase 2B — admin-initiated provisioning: creates the identity + role
    /// assignments in <see cref="AccountLifecycleSnapshot.Provisioned"/>
    /// state. NO password is set, email is unverified, NO activation token
    /// is issued, NO email is sent. A subsequent explicit call to
    /// <see cref="MarkPendingActivationAsync"/> (driven by the
    /// <c>SendActivationEmailCommand</c> use case) is required before the
    /// invitee can complete onboarding via <see cref="CompleteActivationAsync"/>.
    /// <para>
    /// Separating provision from send-activation lets admins pre-create
    /// accounts ahead of time without blasting invite emails, and keeps the
    /// lifecycle transition authoritative: <c>Provisioned → PendingActivation</c>
    /// happens only after the email is actually dispatched.
    /// </para>
    /// </summary>
    Task<Result<Guid>> RegisterProvisionedAsync(
        InvitedUserRegistrationRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Transitions an identity from <see cref="AccountLifecycleSnapshot.Provisioned"/>
    /// (or idempotently from <see cref="AccountLifecycleSnapshot.PendingActivation"/>)
    /// to <see cref="AccountLifecycleSnapshot.PendingActivation"/>. Used by
    /// the <c>SendActivationEmailCommand</c> pipeline after email delivery
    /// succeeds, so the on-record lifecycle never advances past
    /// <c>Provisioned</c> unless an activation email has actually been sent.
    /// <para>
    /// Idempotent. Returns <c>NotFound</c> if the user does not exist.
    /// Returns <c>Conflict</c> if the account is in a later lifecycle state
    /// (Active, Suspended, PendingPasswordReset, Archived).
    /// </para>
    /// </summary>
    Task<Result> MarkPendingActivationAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Phase 3A — transitions an <see cref="AccountLifecycleSnapshot.Active"/>
    /// account to <see cref="AccountLifecycleSnapshot.PendingPasswordReset"/>.
    /// Used by the admin-initiated password-reset flow
    /// (<c>AdminResetPasswordCommand</c>) to block login until the user
    /// completes the reset via the emailed code.
    /// <para>
    /// Idempotent. Outcome mapping:
    /// </para>
    /// <list type="bullet">
    ///   <item><description><c>NotFound</c> — user does not exist.</description></item>
    ///   <item><description><c>Success</c> — transition performed (or no-op if already <c>PendingPasswordReset</c>).</description></item>
    ///   <item><description><c>Conflict</c> — current state does not allow admin reset (Provisioned / PendingActivation / Suspended / Archived).</description></item>
    /// </list>
    /// <para>
    /// The completion leg — transitioning <c>PendingPasswordReset → Active</c>
    /// once the user successfully sets a new password — lives inside
    /// <see cref="ISecurityService.ReplacePasswordBySelfAsync"/> (auto-clear)
    /// so the reset commit stays atomic.
    /// </para>
    /// </summary>
    Task<Result> MarkPendingPasswordResetAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists active roles that the current authenticated inviter is allowed to
    /// pre-assign during Invite User flow.
    /// </summary>
    Task<Result<IReadOnlyList<InvitableRoleOption>>> ListInvitableRolesAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the onboarding status of the account identified by email, or
    /// <c>null</c> if no such account exists. Used by Auth to gate invite
    /// accept/resend flows without exposing the full User aggregate.
    /// </summary>
    Task<InviteAccountStatus?> GetInviteAccountStatusAsync(
        string email,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Phase 2B — finalize activation for an account currently in
    /// <see cref="AccountLifecycleSnapshot.PendingActivation"/>. In a single
    /// unit of work: sets the initial password, marks the primary email
    /// verified, and performs the explicit
    /// <c>PendingActivation → Active</c> lifecycle transition. Fails if the
    /// user does not exist, the email does not match, or the account is
    /// already fully onboarded.
    /// </summary>
    Task<Result> CompleteActivationAsync(
        Guid userId,
        string email,
        string password,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Legacy invite finalization (Phase 2A). Semantically identical to
    /// <see cref="CompleteActivationAsync"/>; retained as a shim for the
    /// legacy <c>AcceptInviteCommand</c> façade. New code should call
    /// <see cref="CompleteActivationAsync"/> directly.
    /// </summary>

    /// <summary>
    /// External-provider first-login provisioning.
    /// <para>
    /// Creates a fully-onboarded local identity seeded from a provider-verified
    /// profile: primary email marked verified, account active, NO usable
    /// password (login is gated to external providers until the user sets a
    /// local password through password-reset). The caller MUST have already
    /// verified that the provider asserted <c>email_verified=true</c> and
    /// that no local account exists with this email — this method refuses to
    /// overwrite or re-use an existing email.
    /// </para>
    /// </summary>
    Task<Result<Guid>> RegisterExternalAsync(
        ExternalUserRegistrationRequest request,
        CancellationToken cancellationToken = default);
}
