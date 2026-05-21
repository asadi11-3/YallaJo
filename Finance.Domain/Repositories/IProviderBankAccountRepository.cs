using Finance.Domain.Entities;
using YallaJo.SharedKernel.Domain.Abstractions.Data;

namespace Finance.Domain.Repositories;

/// <summary>
/// Read-side queries for <see cref="ProviderBankAccount"/> aggregate.
/// </summary>
/// <remarks>
/// Current schema uses <c>UserId</c> to identify the provider. T4 will rename to <c>ProviderId</c>
/// once the provider concept is fully separated from the user identity.
/// </remarks>
public interface IProviderBankAccountRepository : IRepository<ProviderBankAccount, Guid>
{
    /// <summary>Returns the default verified bank account for the provider+currency, or null if none.</summary>
    Task<ProviderBankAccount?> GetDefaultForProviderAsync(
        Guid providerId,
        string currency,
        CancellationToken ct = default);

    /// <summary>Returns all verified bank accounts for the provider+currency.</summary>
    Task<IReadOnlyList<ProviderBankAccount>> GetVerifiedForProviderAsync(
        Guid providerId,
        string currency,
        CancellationToken ct = default);
}
