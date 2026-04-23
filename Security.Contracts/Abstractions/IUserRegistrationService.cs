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
    /// Admin-initiated invited registration: NO password is set, the email is
    /// unverified, the account is inactive. Finalization happens later through
    /// <see cref="CompleteInviteAsync"/> once the invitee proves mailbox
    /// ownership via the invite-acceptance flow.
    /// </summary>
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
    /// Finalize an invited account in a single step: set the password, mark
    /// the primary email verified, and activate the account. Fails if the
    /// user does not exist or is already fully onboarded.
    /// </summary>
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

public sealed record InviteAccountStatus(
    Guid UserId,
    string Email,
    bool IsEmailVerified,
    bool IsActive);

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
