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
}
