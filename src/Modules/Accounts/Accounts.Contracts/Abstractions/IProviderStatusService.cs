namespace Accounts.Contracts.Abstractions;

/// <summary>
/// Cross-module contract allowing other modules to check whether
/// a user holds an approved provider application.
/// </summary>
public interface IProviderStatusService
{
    /// <summary>
    /// Returns <c>true</c> when the user identified by <paramref name="userId"/>
    /// owns a <see cref="ProviderApplicationStatus.Approved"/> provider application.
    /// </summary>
    Task<bool> IsApprovedProviderAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Enumerates the (owner user id, provider id) pairs for every currently-approved
    /// provider application. Used by the Security module to backfill the
    /// server-generated <c>provider_id</c> identity claim for providers that were
    /// approved before the claim-issuing logic existed.
    /// </summary>
    /// <remarks>
    /// <c>ProviderId</c> is the <c>ProviderApplication.Id</c> — the canonical provider
    /// identifier used across Booking, Finance and ContentTours.
    /// </remarks>
    Task<IReadOnlyList<ApprovedProviderClaim>> GetApprovedProviderClaimsAsync(
        CancellationToken cancellationToken = default);
}

/// <summary>
/// A single approved provider's (owner user, provider) identity pair used to
/// issue/backfill the <c>provider_id</c> claim.
/// </summary>
public sealed record ApprovedProviderClaim(Guid UserId, Guid ProviderId);
