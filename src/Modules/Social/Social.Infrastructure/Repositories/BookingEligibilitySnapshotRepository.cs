using Microsoft.EntityFrameworkCore;
using Social.Domain.Entities;
using Social.Domain.Enums;
using Social.Domain.Repositories;
using Social.Infrastructure.Persistence;

namespace Social.Infrastructure.Repositories;

internal sealed class BookingEligibilitySnapshotRepository(SocialDbContext context)
    : IBookingEligibilitySnapshotRepository
{
    public Task<BookingEligibilitySnapshot?> GetAsync(
        Guid userId, ReviewTargetType targetType, Guid targetId, CancellationToken ct = default)
        => context.BookingEligibilitySnapshots
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.UserId == userId
                                      && s.TargetType == targetType
                                      && s.TargetId == targetId, ct);

    public async Task UpsertAsync(BookingEligibilitySnapshot snapshot, CancellationToken ct = default)
    {
        var existing = await context.BookingEligibilitySnapshots
            .FirstOrDefaultAsync(s => s.UserId == snapshot.UserId
                                      && s.TargetType == snapshot.TargetType
                                      && s.TargetId == snapshot.TargetId, ct)
            .ConfigureAwait(false);

        if (existing is null)
        {
            await context.BookingEligibilitySnapshots.AddAsync(snapshot, ct).ConfigureAwait(false);
        }
        else
        {
            // Update in-place by mirroring snapshot state
            existing.RecordBooking(snapshot.LastCompletedAt);
        }
    }
}
