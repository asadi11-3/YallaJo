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
    /// Admin-initiated invited registration (legacy, Phase 2A).
    /// <para>
    /// Equivalent to <see cref="RegisterProvisionedAsync"/> immediately
    /// followed by <see cref="MarkPendingActivationAsync"/>. Kept as a shim
    /// so the legacy <c>InviteUserCommand</c> façade and any other
    /// pre-Phase-2B callers keep compiling; new code should prefer the
    /// split verbs.
    /// </para>
    /// </summary>
    [Obsolete("Use RegisterProvisionedAsync (then MarkPendingActivationAsync once the activation email is dispatched). Kept for Phase 2A+2B backward compatibility; will be removed in Phase 4.")]
    Task<Result<Guid>> RegisterInvitedAsync(
        InvitedUserRegistrationRequest request,
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
    [Obsolete("Use CompleteActivationAsync. Kept for Phase 2A+2B backward compatibility; will be removed in Phase 4.")]
    Task<Result> CompleteInviteAsync(
        Guid userId,
        string email,
        string password,
        CancellationToken cancellationToken = default);

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

public sealed record UserRegistrationRequest(
    string FirstName,
    string LastName,
    string Email,
    string Password);

public sealed record InvitedUserRegistrationRequest(
    string FirstName,
    string LastName,
    string Email,
    IReadOnlyList<Guid> InitialRoleIds);

public sealed record InvitableRoleOption(
    Guid RoleId,
    string Name,
    string? Description,
    bool IsPrivileged);

/// <summary>
/// Lightweight onboarding snapshot returned by
/// <see cref="IUserRegistrationService.GetInviteAccountStatusAsync"/>. Used
/// by Auth's invite/activation flows to gate resend/accept/send-activation
/// without exposing the full User aggregate.
/// <para>
/// Phase 2B: <see cref="Lifecycle"/> is the authoritative gate.
/// <see cref="IsActive"/> is retained as a derived convenience for existing
/// callers (it equals <c>Lifecycle == Active</c>); defaults to
/// <see cref="AccountLifecycleSnapshot.Provisioned"/> to keep pre-2B test
/// doubles source-compatible.
/// </para>
/// </summary>
public sealed record InviteAccountStatus(
    Guid UserId,
    string Email,
    bool IsEmailVerified,
    bool IsActive,
    AccountLifecycleSnapshot Lifecycle = AccountLifecycleSnapshot.Provisioned);

/// <summary>
/// Seed data for <see cref="IUserRegistrationService.RegisterExternalAsync"/>.
/// <para>
/// All fields come from the OAuth provider's claims — the caller is responsible
/// for confirming that the provider itself attested email verification
/// (e.g. Google's <c>email_verified=true</c>, or Meta returning an email at all).
/// </para>
/// </summary>
public sealed record ExternalUserRegistrationRequest(
    string Email,
    string FirstName,
    string LastName);
