using ContentTours.Domain.Entities;
using ContentTours.Domain.Enums;
using ContentTours.Domain.Repositories;
using ContentTours.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using YallaJo.SharedKernel.Infrastructure.Data.Repositories;

namespace ContentTours.Infrastructure.Repositories;

internal sealed class TourRepository(ContentToursDbContext context)
    : EfRepository<Tour, Guid>(context), ITourRepository
{
    public Task<bool> IsSlugReservedAsync(string slug, Guid? excludeTourId, CancellationToken ct = default)
    {
        var normalized = NormalizeSlug(slug);
        var graceCutoff = DateTime.UtcNow.AddDays(-30);

        var query = context.Tours
            .IgnoreQueryFilters()
            .Where(t => t.Slug == normalized
                && (!t.IsDeleted || (t.DeletedAt != null && t.DeletedAt >= graceCutoff)));

        if (excludeTourId.HasValue)
            query = query.Where(t => t.Id != excludeTourId.Value);

        return query.AnyAsync(ct);
    }

    public async Task<bool> HasFutureSchedulesAsync(Guid tourId, CancellationToken ct = default)
    {
        var hasActiveSchedule = await context.TourSchedules
            .AnyAsync(s => s.TourId == tourId && s.IsActive, ct)
            .ConfigureAwait(false);
        if (hasActiveSchedule) return true;

        var now = DateTime.UtcNow;
        return await context.TourPackageTours
            .AnyAsync(
                link => link.TourId == tourId
                  && link.TourPackage.IsActive
                  && (link.TourPackage.ValidTo == null || link.TourPackage.ValidTo >= now),
                ct)
            .ConfigureAwait(false);
    }

    public Task<bool> HasActivePricingAsync(Guid tourId, CancellationToken ct = default) =>
        context.TourPricingTiers.AnyAsync(p => p.TourId == tourId && p.IsActive, ct);

    public Task<bool> HasActiveAdultPricingAsync(Guid tourId, CancellationToken ct = default) =>
        context.TourPricingTiers
            .AnyAsync(p => p.TourId == tourId && p.IsActive && p.ParticipantType == ParticipantType.Adult, ct);

    public Task<bool> HasActiveScheduleAsync(Guid tourId, CancellationToken ct = default) =>
        context.TourSchedules.AnyAsync(s => s.TourId == tourId && s.IsActive, ct);

    public async Task<IReadOnlyList<Tour>> GetByIdsIncludingDeletedAsync(
        IReadOnlyCollection<Guid> ids,
        CancellationToken ct = default)
    {
        if (ids is null || ids.Count == 0)
        {
            return Array.Empty<Tour>();
        }

        return await context.Tours
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(t => ids.Contains(t.Id))
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    private static string NormalizeSlug(string slug) =>
        (slug ?? string.Empty).Trim().ToLowerInvariant();
}
