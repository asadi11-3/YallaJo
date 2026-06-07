using Accounts.Contracts.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Security.Contracts.Authorization;
using Security.Domain.Entities;
using YallaJo.SharedKernel.Infrastructure.Data;

namespace Security.Infrastructure.Persistence.Seeding;

/// <summary>
/// One-time backfill of the server-generated <c>provider_id</c> identity claim for
/// providers that were approved BEFORE the claim-issuing logic existed.
/// </summary>
/// <remarks>
/// <para>
/// Runs after the Accounts provider seeders (Order 51) so any seeded approved providers
/// are present. Idempotent: a provider that already has its <c>provider_id</c> claim is
/// skipped, so re-running on every startup is safe and a no-op once backfilled.
/// </para>
/// <para>
/// The claim value is the approved <c>ProviderApplication.Id</c> (the canonical ProviderId),
/// resolved server-side via <see cref="IProviderStatusService"/> — never sourced from a client.
/// Existing signed-in providers must refresh their token / re-login to pick up the new claim,
/// because the JWT is only re-minted on login or token refresh.
/// </para>
/// </remarks>
public sealed class ProviderIdClaimBackfillInitializer(
    SecurityDbContext dbContext,
    IProviderStatusService providerStatusService,
    ILogger<ProviderIdClaimBackfillInitializer> logger) : IModuleDbInitializer
{
    public int Order => 52;

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        var approved = await providerStatusService.GetApprovedProviderClaimsAsync(cancellationToken);
        if (approved.Count == 0)
        {
            return;
        }

        // Pre-load the existing provider_id claims so the backfill is a single round-trip
        // regardless of how many approved providers exist.
        var existing = await dbContext.UserClaims
            .Where(c => c.ClaimType == ProviderClaimTypes.ProviderId)
            .Select(c => new { c.UserId, c.ClaimValue })
            .ToListAsync(cancellationToken);

        var existingSet = existing
            .Select(c => (c.UserId, c.ClaimValue))
            .ToHashSet();

        var toAdd = new List<UserClaim>();
        foreach (var provider in approved)
        {
            var claimValue = provider.ProviderId.ToString();
            if (existingSet.Contains((provider.UserId, claimValue)))
            {
                continue;
            }

            toAdd.Add(UserClaim.Create(provider.UserId, ProviderClaimTypes.ProviderId, claimValue));
        }

        if (toAdd.Count == 0)
        {
            return;
        }

        dbContext.UserClaims.AddRange(toAdd);
        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Security: Backfilled provider_id claim for {Count} approved provider(s).",
            toAdd.Count);
    }
}
