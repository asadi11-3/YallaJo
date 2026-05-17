using Booking.Domain.Entities;
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
}
