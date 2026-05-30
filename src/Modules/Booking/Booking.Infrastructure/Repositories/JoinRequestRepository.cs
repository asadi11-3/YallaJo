using Booking.Domain.Entities;
using Booking.Domain.Repositories;
using Booking.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using YallaJo.SharedKernel.Infrastructure.Data.Repositories;

namespace Booking.Infrastructure.Repositories;

internal sealed class JoinRequestRepository(BookingDbContext context)
    : EfRepository<JoinRequest, Guid>(context), IJoinRequestRepository
{
    public async Task<IReadOnlyList<JoinRequest>> GetByBookingIdAsync(Guid bookingId, CancellationToken ct = default)
        => await context.JoinRequests.Where(j => j.TourBookingId == bookingId).ToListAsync(ct);

    public async Task<IReadOnlyList<JoinRequest>> GetByUserIdAsync(Guid userId, CancellationToken ct = default)
        => await context.JoinRequests.Where(j => j.UserId == userId).ToListAsync(ct);

    public async Task<IReadOnlyList<JoinRequest>> GetExpiredPendingAsync(DateTime utcNow, CancellationToken ct = default)
        => await context.JoinRequests
            .Where(j => j.Status == Booking.Domain.Enums.JoinRequestStatus.Pending && j.ExpiresAt <= utcNow)
            .ToListAsync(ct);

    public async Task<bool> HasPendingRequestAsync(Guid tourBookingId, Guid userId, CancellationToken ct = default)
        => await context.JoinRequests
            .AnyAsync(j => j.TourBookingId == tourBookingId && j.UserId == userId && j.Status == Booking.Domain.Enums.JoinRequestStatus.Pending, ct);
}
