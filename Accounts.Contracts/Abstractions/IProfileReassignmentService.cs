using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Accounts.Contracts.Abstractions;

/// <summary>
/// Phase 3D — cross-module capability exposed by Accounts so the Auth
/// admin-reassignment flow can scrub the target user's profile inside
/// the same transactional scope as the Security/Auth reassignment.
/// <para>
/// Keeps the <c>Profile</c> aggregate inside the Accounts module —
/// callers pass the <c>UserId</c> + new primary email and receive a
/// <see cref="Result"/>. The implementation loads the tracked profile,
/// calls <c>Profile.ResetForReassignment</c>, persists via
/// <c>IAccountsUnitOfWork</c>, and evicts the profile cache tag.
/// </para>
/// <para>
/// Outcomes:
/// </para>
/// <list type="bullet">
///   <item><description><c>Success</c> — profile found and reset.</description></item>
///   <item><description><c>Success</c> (logged no-op) — no profile row exists for <paramref name="request"/>.<c>UserId</c>; reassignment must not fail just because the profile has not been provisioned yet. A subsequent <c>UserCreatedIntegrationEvent</c> replay would create a fresh clean profile anyway.</description></item>
///   <item><description>Exceptions (persistence, concurrency) propagate — the ambient <c>TransactionScope</c> in the caller (<c>AdminReassignAccountCommandHandler</c>) rolls back Security + Auth reassignment atomically.</description></item>
/// </list>
/// </summary>
public interface IProfileReassignmentService
{
    Task<Result> ResetForReassignmentAsync(
        ProfileReassignmentRequest request,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Phase 3D — input payload for
/// <see cref="IProfileReassignmentService.ResetForReassignmentAsync"/>.
/// </summary>
/// <param name="UserId">The reassigned account's stable Security.User id.</param>
/// <param name="NewEmail">
/// The new primary email assigned by the admin. The service derives
/// the profile's interim <c>DisplayName</c> from the local part
/// (portion before <c>@</c>) so the admin UI sees a neutral identifier
/// until the new owner completes activation.
/// </param>
public sealed record ProfileReassignmentRequest(
    Guid UserId,
    string NewEmail);
