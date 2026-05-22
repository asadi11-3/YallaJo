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
}
