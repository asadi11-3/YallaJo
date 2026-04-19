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
}

public sealed record UserRegistrationRequest(
    string FirstName,
    string LastName,
    string Email,
    string Password);

public sealed record InvitedUserRegistrationRequest(
    string FirstName,
    string LastName,
    string Email);

public sealed record InviteAccountStatus(
    Guid UserId,
    string Email,
    bool IsEmailVerified,
    bool IsActive);
