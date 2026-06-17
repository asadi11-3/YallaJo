using Accounts.Domain.Entities;
using Accounts.Domain.Enums;
using Accounts.Domain.Repositories;
using Accounts.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using YallaJo.SharedKernel.Infrastructure.Data.Repositories;

namespace Accounts.Infrastructure.Repositories;

public sealed class ProviderApplicationRepository(AccountsDbContext context)
    : EfRepository<ProviderApplication, Guid>(context), IProviderApplicationRepository
{
    public async Task<ProviderApplication?> GetByIdAsync(Guid applicationId, CancellationToken ct = default)
        => await context.ProviderApplications
            .FirstOrDefaultAsync(a => a.Id == applicationId, ct);

    public async Task<ProviderApplication?> GetByUserIdAsync(Guid userId, CancellationToken ct = default)
        => await context.ProviderApplications
            .FirstOrDefaultAsync(a => a.UserId == userId, ct);

    public async Task<ProviderApplication?> GetWithDocumentsAsync(Guid applicationId, CancellationToken ct = default)
        => await context.ProviderApplications
            .Include(a => a.Documents)
            .FirstOrDefaultAsync(a => a.Id == applicationId, ct);

    public async Task<ProviderApplication?> GetWithDocumentsByUserIdAsync(Guid userId, CancellationToken ct = default)
        => await context.ProviderApplications
            .Include(a => a.Documents)
            .FirstOrDefaultAsync(a => a.UserId == userId, ct);

    public async Task<ProviderApplication?> GetWithDocumentsByDocumentIdAsync(Guid documentId, CancellationToken ct = default)
        => await context.ProviderApplications
            .Include(a => a.Documents)
            .FirstOrDefaultAsync(a => a.Documents.Any(d => d.Id == documentId), ct);

    public async Task<bool> ExistsApprovedForUserAsync(Guid userId, CancellationToken ct = default)
        => await context.ProviderApplications
            .AnyAsync(a => a.UserId == userId && a.Status == ProviderApplicationStatus.Approved, ct);

    public async Task<IReadOnlyList<(Guid UserId, Guid ProviderId)>> GetApprovedUserProviderPairsAsync(
        CancellationToken ct = default)
    {
        var rows = await context.ProviderApplications
            .Where(a => a.Status == ProviderApplicationStatus.Approved)
            .Select(a => new { a.UserId, a.Id })
            .AsNoTracking()
            .ToListAsync(ct);

        return rows.Select(r => (r.UserId, r.Id)).ToList();
    }

    public async Task<IReadOnlyList<ProviderApplication>> GetQueueAsync(
        ProviderApplicationStatus? statusFilter,
        ProviderType? typeFilter,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        var query = context.ProviderApplications.AsQueryable();

        if (statusFilter.HasValue)
            query = query.Where(a => a.Status == statusFilter.Value);

        if (typeFilter.HasValue)
            query = query.Where(a => a.Type == typeFilter.Value);

        return await query
            .OrderByDescending(a => a.SubmittedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);
    }

    public async Task<int> GetQueueCountAsync(
        ProviderApplicationStatus? statusFilter,
        ProviderType? typeFilter,
        CancellationToken ct = default)
    {
        var query = context.ProviderApplications.AsQueryable();

        if (statusFilter.HasValue)
            query = query.Where(a => a.Status == statusFilter.Value);

        if (typeFilter.HasValue)
            query = query.Where(a => a.Type == typeFilter.Value);

        return await query.CountAsync(ct);
    }

    public async Task<IReadOnlyDictionary<ProviderApplicationStatus, int>> GetStatusCountsAsync(
        CancellationToken ct = default)
        => await context.ProviderApplications
            .GroupBy(a => a.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .AsNoTracking()
            .ToDictionaryAsync(x => x.Status, x => x.Count, ct);

    public async Task<IReadOnlyList<ProviderApplication>> GetApprovedWithExpiringDocumentsAsync(
        DateTime expiryThreshold,
        CancellationToken ct = default)
        => await context.ProviderApplications
            .Include(a => a.Documents)
            .Where(a => a.Status == ProviderApplicationStatus.Approved)
            .Where(a => a.Documents.Any(d => d.ExpiresAt.HasValue && d.ExpiresAt.Value <= expiryThreshold))
            .AsNoTracking()
            .ToListAsync(ct);

    public async Task<IReadOnlyList<ProviderDocument>> GetDocumentsForBackfillAsync(
        Guid afterId,
        int take,
        CancellationToken ct = default)
    {
        // Keyset pagination on Id (GUIDv7 monotonic). The HasQueryFilter on
        // ProviderDocument (!Application.IsDeleted) auto-applies via the navigation
        // include. AsNoTracking: the backfill never mutates ProviderDocument.
        if (take <= 0)
            return Array.Empty<ProviderDocument>();

        return await context.ProviderDocuments
            .AsNoTracking()
            .Include(d => d.Application)
            .Where(d => d.Id.CompareTo(afterId) > 0)
            .OrderBy(d => d.Id)
            .Take(take)
            .ToListAsync(ct);
    }

    public async Task<Guid?> GetFileAssetIdByDocumentIdAsync(
        Guid providerDocumentId,
        CancellationToken ct = default)
    {
        // Patch 2C download switch: single projected AsNoTracking lookup against the
        // accounts.ProviderDocumentFiles link table (unique on ProviderDocumentId per
        // UX_ProviderDocumentFiles_ProviderDocumentId). Returns null when no link row
        // exists yet — the caller falls back to the legacy ProviderDocument.FileUrl
        // path. The FileAssetId is opaque here; no DB FK to content_core.FileAssets.
        return await context.Set<ProviderDocumentFile>()
            .AsNoTracking()
            .Where(f => f.ProviderDocumentId == providerDocumentId)
            .Select(f => (Guid?)f.FileAssetId)
            .FirstOrDefaultAsync(ct);
    }

    public async Task<IReadOnlyDictionary<Guid, Guid>> GetFileAssetIdsByDocumentIdsAsync(
        IReadOnlyCollection<Guid> documentIds,
        CancellationToken ct = default)
    {
        // Patch 2E list/index switch: single batched AsNoTracking lookup against the
        // accounts.ProviderDocumentFiles link table for the whole document set, to avoid
        // N+1 when projecting a document list. Documents without a link row are simply
        // absent from the result (caller falls back to legacy ProviderDocument metadata).
        // UX_ProviderDocumentFiles_ProviderDocumentId guarantees one FileAssetId per doc,
        // so ToDictionary cannot collide. FileAssetId is opaque; no DB FK to FileAssets.
        var ids = documentIds.Where(id => id != Guid.Empty).Distinct().ToArray();
        if (ids.Length == 0)
            return new Dictionary<Guid, Guid>();

        var rows = await context.Set<ProviderDocumentFile>()
            .AsNoTracking()
            .Where(f => ids.Contains(f.ProviderDocumentId))
            .Select(f => new { f.ProviderDocumentId, f.FileAssetId })
            .ToListAsync(ct);

        return rows.ToDictionary(x => x.ProviderDocumentId, x => x.FileAssetId);
    }
}
