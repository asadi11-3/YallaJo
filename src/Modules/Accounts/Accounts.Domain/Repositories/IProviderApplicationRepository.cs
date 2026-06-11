using Accounts.Domain.Entities;
using Accounts.Domain.Enums;
using YallaJo.SharedKernel.Domain.Abstractions.Data;

namespace Accounts.Domain.Repositories;

public interface IProviderApplicationRepository : IRepository<ProviderApplication, Guid>
{
    Task<ProviderApplication?> GetByIdAsync(Guid applicationId, CancellationToken ct = default);

    Task<ProviderApplication?> GetByUserIdAsync(Guid userId, CancellationToken ct = default);

    Task<ProviderApplication?> GetWithDocumentsAsync(Guid applicationId, CancellationToken ct = default);

    Task<ProviderApplication?> GetWithDocumentsByUserIdAsync(Guid userId, CancellationToken ct = default);

    Task<bool> ExistsApprovedForUserAsync(Guid userId, CancellationToken ct = default);

    /// <summary>
    /// Returns the (owner user id, application id) pairs for every approved provider
    /// application. The application id is the canonical ProviderId. Used for backfilling
    /// the server-generated <c>provider_id</c> identity claim.
    /// </summary>
    Task<IReadOnlyList<(Guid UserId, Guid ProviderId)>> GetApprovedUserProviderPairsAsync(
        CancellationToken ct = default);

    Task<IReadOnlyList<ProviderApplication>> GetQueueAsync(
        ProviderApplicationStatus? statusFilter,
        ProviderType? typeFilter,
        int page,
        int pageSize,
        CancellationToken ct = default);

    Task<int> GetQueueCountAsync(
        ProviderApplicationStatus? statusFilter,
        ProviderType? typeFilter,
        CancellationToken ct = default);

    /// <summary>
    /// Returns the number of provider applications per status (single grouped query).
    /// Used by the admin queue's counted status tabs.
    /// </summary>
    Task<IReadOnlyDictionary<ProviderApplicationStatus, int>> GetStatusCountsAsync(
        CancellationToken ct = default);

    Task<IReadOnlyList<ProviderApplication>> GetApprovedWithExpiringDocumentsAsync(
        DateTime expiryThreshold,
        CancellationToken ct = default);
}
