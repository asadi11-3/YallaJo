using Booking.Domain.Entities;
using Booking.Domain.Repositories;
using Booking.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using YallaJo.SharedKernel.Infrastructure.Data.Repositories;

namespace Booking.Infrastructure.Repositories;

internal sealed class GuideDiscountRepository(BookingDbContext context)
    : EfRepository<GuideDiscount, Guid>(context), IGuideDiscountRepository
{
    public async Task<IReadOnlyList<GuideDiscount>> GetActiveByGuideAsync(
        Guid guideUserId,
        Guid? tourId,
        DateTime utcNow,
        CancellationToken ct = default)
    {
        var query = context.GuideDiscounts
            .Where(d => d.GuideUserId == guideUserId
                     && d.IsActive
                     && d.ValidFrom <= utcNow
                     && (!d.ValidUntil.HasValue || d.ValidUntil.Value >= utcNow));

        if (tourId.HasValue)
        {
            query = query.Where(d => d.TourId == null || d.TourId == tourId.Value);
        }

        return await query.OrderByDescending(d => d.CreatedAt).ToListAsync(ct);
    }

    public async Task<IReadOnlyList<GuideDiscount>> GetByGuideUserIdAsync(Guid guideUserId, CancellationToken ct = default)
        => await context.GuideDiscounts
            .Where(d => d.GuideUserId == guideUserId)
            .OrderByDescending(d => d.CreatedAt)
            .ToListAsync(ct);
}
