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
}
