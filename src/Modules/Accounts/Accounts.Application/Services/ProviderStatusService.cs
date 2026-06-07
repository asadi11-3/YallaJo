using Accounts.Contracts.Abstractions;
using Accounts.Domain.Repositories;

namespace Accounts.Application.Services;

internal sealed class ProviderStatusService(
    IProviderApplicationRepository providerApplicationRepository)
    : IProviderStatusService
{
    public Task<bool> IsApprovedProviderAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return providerApplicationRepository.ExistsApprovedForUserAsync(userId, cancellationToken);
    }

    public async Task<IReadOnlyList<ApprovedProviderClaim>> GetApprovedProviderClaimsAsync(
        CancellationToken cancellationToken = default)
    {
        var pairs = await providerApplicationRepository
            .GetApprovedUserProviderPairsAsync(cancellationToken);

        return pairs
            .Select(p => new ApprovedProviderClaim(p.UserId, p.ProviderId))
            .ToList();
    }
}
