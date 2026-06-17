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

    /// <summary>
    /// Loads the provider application (including its documents) that OWNS the given
    /// document id, regardless of which user it belongs to. Read-only lookup used by
    /// the authorized document-download path for admin-tier callers. Returns null when
    /// no non-deleted application contains a document with that id.
    /// </summary>
    Task<ProviderApplication?> GetWithDocumentsByDocumentIdAsync(Guid documentId, CancellationToken ct = default);

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

    /// <summary>
    /// Returns the FileAssetId linked to the given <paramref name="providerDocumentId"/>
    /// via the <c>accounts.ProviderDocumentFiles</c> table (Patch 2C download switch),
    /// or <c>null</c> when no link row exists yet (e.g. the Patch 2B backfill has not
    /// linked this document). The FileAssetId is an opaque cross-module reference into
    /// <c>content_core.FileAssets</c>; resolution to a usable <c>FileAssetView</c> is
    /// performed via <c>IFileAssetLocator</c>. No FK is enforced at the database level
    /// by design (module boundary, see m0336 / Patch 2A).
    /// </summary>
    Task<Guid?> GetFileAssetIdByDocumentIdAsync(
        Guid providerDocumentId,
        CancellationToken ct = default);

    /// <summary>
    /// Batch variant of <see cref="GetFileAssetIdByDocumentIdAsync"/> (Patch 2E list/index
    /// read-model switch). Returns a map of provider-document id to its linked FileAssetId
    /// via the <c>accounts.ProviderDocumentFiles</c> table, for the subset of the supplied
    /// <paramref name="documentIds"/> that have a link row. Documents without a link are
    /// simply absent from the map (caller falls back to legacy ProviderDocument metadata).
    /// Executes a single round-trip to avoid N+1 when projecting a document list. The
    /// unique index <c>UX_ProviderDocumentFiles_ProviderDocumentId</c> guarantees at most
    /// one FileAssetId per document. The FileAssetId is an opaque cross-module reference
    /// into <c>content_core.FileAssets</c> (no DB FK by design, module boundary).
    /// </summary>
    Task<IReadOnlyDictionary<Guid, Guid>> GetFileAssetIdsByDocumentIdsAsync(
        IReadOnlyCollection<Guid> documentIds,
        CancellationToken ct = default);
}
