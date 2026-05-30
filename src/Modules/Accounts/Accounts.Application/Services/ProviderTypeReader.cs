using Accounts.Contracts.Abstractions;
using Accounts.Domain.Enums;
using Accounts.Domain.Repositories;

namespace Accounts.Application.Services;

internal sealed class ProviderTypeReader(
    IProviderApplicationRepository providerApplicationRepository)
    : IProviderTypeReader
{
    public async Task<ProviderTypeValue?> GetProviderTypeAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var application = await providerApplicationRepository.GetByUserIdAsync(userId, cancellationToken);

        if (application is null || application.Status != ProviderApplicationStatus.Approved)
        {
            return null;
        }

        return (ProviderTypeValue)(int)application.Type;
    }
}
