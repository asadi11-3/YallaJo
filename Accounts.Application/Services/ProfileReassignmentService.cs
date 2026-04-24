using Accounts.Application.Caching;
using Accounts.Contracts.Abstractions;
using Accounts.Domain.Repositories;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Accounts.Application.Services;

/// <summary>
/// Phase 3D implementation of <see cref="IProfileReassignmentService"/>.
/// Keeps the <c>Profile</c> aggregate inside the Accounts module and
/// is invoked by the Auth admin-reassignment command handler inside
/// the existing cross-module <c>ITransactionalExecutor</c> scope.
/// <para>
/// The service:
/// </para>
/// <list type="number">
///   <item><description>Loads the tracked <c>Profile</c> row by <c>UserId</c> (the unique index <c>IX_Profiles_UserId_Unique</c> guarantees at most one match).</description></item>
///   <item><description>If no profile exists, returns <see cref="Result.Success"/> and logs a no-op — reassignment must remain idempotent with respect to not-yet-provisioned profiles (outbox lag, partially onboarded users).</description></item>
///   <item><description>Otherwise calls <c>Profile.ResetForReassignment</c> with the local-part of <paramref name="request"/>.<c>NewEmail</c> (portion before the first <c>@</c>).</description></item>
///   <item><description>Persists via <see cref="IAccountsUnitOfWork.SaveChangesAsync"/> — enlists in the ambient <c>TransactionScope</c> so a rollback by the outer handler also rolls back this change.</description></item>
///   <item><description>Evicts <see cref="AccountsCacheKeys.UserProfileTag"/> so the next read reflects the scrubbed row.</description></item>
/// </list>
/// <para>
/// Persistence / concurrency exceptions are NOT swallowed — they
/// propagate out so the outer <c>TransactionScope</c> disposes without
/// <c>Complete()</c>, rolling back the entire reassignment. This
/// satisfies the Phase 3D invariant "if profile reset fails,
/// reassignment does not silently succeed".
/// </para>
/// </summary>
internal sealed class ProfileReassignmentService(
    IProfileRepository profileRepository,
    IAccountsUnitOfWork unitOfWork,
    HybridCache cache,
    ILogger<ProfileReassignmentService> logger)
    : IProfileReassignmentService
{
    public async Task<Result<ProfileReassignmentOutcome>> ResetForReassignmentAsync(
        ProfileReassignmentRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request is null)
        {
            return Result<ProfileReassignmentOutcome>.Failure(
                Error.Validation("Profile.Request", "Reassignment request is required."),
                Outcome.Invalid);
        }

        if (request.UserId == Guid.Empty)
        {
            return Result<ProfileReassignmentOutcome>.Failure(
                Error.Validation("Profile.UserId", "UserId is required."),
                Outcome.Invalid);
        }

        // Tracked load — we are about to mutate the aggregate and
        // flush via the Accounts UoW (enlisted in the ambient scope).
        var profile = await profileRepository.FirstOrDefaultAsync(
            filter:       p => p.UserId == request.UserId,
            asNoTracking: false,
            ct:           cancellationToken);

        if (profile is null)
        {
            // Idempotent no-op. Not a failure: a reassignment that
            // races ahead of profile provisioning (outbox lag, user
            // whose UserCreatedIntegrationEvent has not landed yet)
            // must not tear down a valid Security/Auth reassignment.
            // Phase 4 — return Scrubbed=false so the audit row's
            // metadata is truthful about the no-op.
            logger.LogInformation(
                "Accounts: No profile row for user {UserId} — ResetForReassignment is a no-op. " +
                "A subsequent UserCreatedIntegrationEvent replay will create a clean profile.",
                request.UserId);
            return Result<ProfileReassignmentOutcome>.Success(new ProfileReassignmentOutcome(Scrubbed: false));
        }

        var localPart = ExtractLocalPart(request.NewEmail);

        profile.ResetForReassignment(localPart);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        // Evict the per-user profile cache tag so the next
        // GetProfile read reflects the scrubbed placeholders.
        await cache.RemoveByTagAsync(
            AccountsCacheKeys.UserProfileTag(request.UserId),
            cancellationToken);

        logger.LogInformation(
            "Accounts: Profile {ProfileId} for user {UserId} reset for reassignment. " +
            "DisplayName={DisplayName}.",
            profile.Id,
            request.UserId,
            profile.DisplayName ?? "(null)");

        return Result<ProfileReassignmentOutcome>.Success(new ProfileReassignmentOutcome(Scrubbed: true));
    }

    /// <summary>
    /// Returns the portion of <paramref name="email"/> before the
    /// first <c>@</c>, or <c>null</c> if the input is null/empty or
    /// has no local part. Null input is tolerated so callers do not
    /// have to pre-validate here — the domain method treats
    /// null/whitespace as "clear DisplayName".
    /// </summary>
    private static string? ExtractLocalPart(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return null;

        var atIndex = email.IndexOf('@');
        if (atIndex <= 0)
            return null;

        var localPart = email[..atIndex].Trim();
        return string.IsNullOrEmpty(localPart) ? null : localPart;
    }
}
