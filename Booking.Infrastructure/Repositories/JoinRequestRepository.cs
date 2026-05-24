using Booking.Domain.Entities;
using Booking.Domain.Enums;
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

    public Task<bool> ExistsPendingForUserAndBookingAsync(
        Guid bookingId,
        Guid userId,
        CancellationToken ct = default)
        => context.JoinRequests
            .AnyAsync(
                j => j.TourBookingId == bookingId
                    && j.UserId == userId
                    && j.Status == JoinRequestStatus.Pending,
                ct);

    public Task<JoinRequest?> GetByIdTrackedAsync(Guid id, CancellationToken ct = default)
        => context.JoinRequests.FirstOrDefaultAsync(j => j.Id == id, ct);
}
