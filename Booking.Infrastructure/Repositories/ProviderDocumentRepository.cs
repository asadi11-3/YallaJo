using Booking.Domain.Entities;
using Booking.Domain.Enums;
using Booking.Domain.Repositories;
using Booking.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using YallaJo.SharedKernel.Infrastructure.Data.Repositories;

namespace Booking.Infrastructure.Repositories;

internal sealed class ProviderDocumentRepository(BookingDbContext context)
    : EfRepository<ProviderDocument, Guid>(context), IProviderDocumentRepository
{
    public async Task<IReadOnlyList<ProviderDocument>> GetByTourGuideIdAsync(Guid tourGuideId, CancellationToken ct = default)
        => await context.ProviderDocuments.Where(d => d.TourGuideId == tourGuideId).ToListAsync(ct);

    public async Task<IReadOnlyList<ProviderDocument>> GetByBusinessIdAsync(Guid businessId, CancellationToken ct = default)
        => await context.ProviderDocuments.Where(d => d.BusinessId == businessId).ToListAsync(ct);

    public async Task<IReadOnlyList<ProviderDocument>> GetExpiringAsync(DateTime threshold, CancellationToken ct = default)
        => await context.ProviderDocuments
            .Where(d => d.ExpiresAt != null && d.ExpiresAt <= threshold)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<ProviderDocument>> GetExpiringSoonAsync(DateTime nowUtc, DateTime thresholdUtc, CancellationToken ct = default)
        => await context.ProviderDocuments
            .Where(d => d.Status == DocumentStatus.Approved
                && d.ExpiresAt != null
                && d.ExpiresAt > nowUtc
                && d.ExpiresAt <= thresholdUtc
                && d.ExpiringNotificationSentAt == null)
            .OrderBy(d => d.ExpiresAt)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<ProviderDocument>> GetNewlyExpiredAsync(DateTime nowUtc, CancellationToken ct = default)
        => await context.ProviderDocuments
            .Where(d => d.Status == DocumentStatus.Approved
                && d.ExpiresAt != null
                && d.ExpiresAt <= nowUtc
                && d.ExpiredNotificationSentAt == null)
            .OrderBy(d => d.ExpiresAt)
            .ToListAsync(ct);


    public async Task<IReadOnlyList<ProviderDocument>> GetForTourGuideAsync(Guid tourGuideId, CancellationToken ct = default)
        => await GetAllAsync(
            filter: d => d.TourGuideId == tourGuideId,
            orderBy: q => q.OrderBy(d => d.DocumentType).ThenByDescending(d => d.CreatedAt),
            asNoTracking: true,
            ct: ct);

    public Task<ProviderDocument?> GetByIdTrackedAsync(Guid id, CancellationToken ct = default)
        => context.ProviderDocuments.FirstOrDefaultAsync(d => d.Id == id, ct);

    public Task<ProviderDocument?> GetByIdForTourGuideAsync(Guid id, Guid tourGuideId, CancellationToken ct = default)
        => FirstOrDefaultAsync(
            d => d.Id == id && d.TourGuideId == tourGuideId,
            ct: ct);

    public Task<bool> ExistsActiveTypeForTourGuideAsync(
        Guid tourGuideId,
        DocumentType type,
        Guid? excludeId,
        CancellationToken ct = default)
        => AnyAsync(
            d => d.TourGuideId == tourGuideId
                && d.DocumentType == type
                && d.Status != DocumentStatus.Rejected
                && (excludeId == null || d.Id != excludeId.Value),
            ct);

    public Task<Guid?> GetTourGuideIdByUserIdAsync(Guid userId, CancellationToken ct = default)
        => context.TourGuides
            .AsNoTracking()
            .Where(g => g.UserId == userId && g.IsActive)
            .Select(g => (Guid?)g.Id)
            .FirstOrDefaultAsync(ct);

    public async Task<IReadOnlyList<ProviderDocument>> GetExpiredCriticalPendingSuspensionAsync(
        IReadOnlyCollection<DocumentType> criticalTypes,
        int batchSize,
        CancellationToken ct = default)
    {
        if (criticalTypes is null || criticalTypes.Count == 0)
        {
            return Array.Empty<ProviderDocument>();
        }

        return await context.ProviderDocuments
            .Where(d => d.Status == DocumentStatus.Expired
                && d.SuspensionDispatchedAt == null
                && criticalTypes.Contains(d.DocumentType))
            .OrderBy(d => d.ExpiredNotificationSentAt ?? d.UpdatedAt ?? d.CreatedAt)
            .Take(batchSize)
            .ToListAsync(ct);
    }
}
