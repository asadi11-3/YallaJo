using Microsoft.EntityFrameworkCore;
using Tracking.Domain.Entities;
using Tracking.Domain.Enums;
using Tracking.Domain.Repositories;
using Tracking.Infrastructure.Persistence;
using YallaJo.SharedKernel.Infrastructure.Data.Repositories;

namespace Tracking.Infrastructure.Repositories;

internal sealed class TrackingSessionRepository(TrackingDbContext context)
    : EfRepository<LiveTrackingSession, Guid>(context), ITrackingSessionRepository
{
    public Task<LiveTrackingSession?> GetByIdWithDetailsAsync(Guid id, CancellationToken ct = default)
        => context.LiveTrackingSessions
            .Include(s => s.LocationSnapshots)
            .Include(s => s.TourCheckpoints)
            .FirstOrDefaultAsync(s => s.Id == id, ct);

    public Task<LiveTrackingSession?> GetActiveByBookingIdAsync(Guid tourBookingId, CancellationToken ct = default)
        => context.LiveTrackingSessions
            .Include(s => s.LocationSnapshots)
            .Include(s => s.TourCheckpoints)
            .FirstOrDefaultAsync(
                s => s.TourBookingId == tourBookingId
                    && (s.Status == SessionStatus.Active || s.Status == SessionStatus.Paused),
                ct);

    public async Task<IReadOnlyList<LiveTrackingSession>> GetByBookingIdAsync(
        Guid tourBookingId,
        CancellationToken ct = default)
        => await context.LiveTrackingSessions
            .AsNoTracking()
            .Where(s => s.TourBookingId == tourBookingId)
            .OrderByDescending(s => s.StartedAt)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<LiveTrackingSession>> GetActiveSessionsByGuideIdAsync(
        Guid tourGuideId,
        CancellationToken ct = default)
        => await context.LiveTrackingSessions
            .AsNoTracking()
            .Where(s => s.TourGuideId == tourGuideId
                && (s.Status == SessionStatus.Active || s.Status == SessionStatus.Paused))
            .OrderByDescending(s => s.StartedAt)
            .ToListAsync(ct);

    public async Task<LocationSnapshot?> GetLatestSnapshotByBookingIdAsync(
        Guid tourBookingId,
        CancellationToken ct = default)
    {
        // Find the active/paused session for this booking, then get its latest snapshot.
        var sessionId = await context.LiveTrackingSessions
            .AsNoTracking()
            .Where(s => s.TourBookingId == tourBookingId
                && (s.Status == SessionStatus.Active || s.Status == SessionStatus.Paused))
            .Select(s => (Guid?)s.Id)
            .FirstOrDefaultAsync(ct);

        if (sessionId is null)
            return null;

        return await context.LocationSnapshots
            .AsNoTracking()
            .Where(snap => snap.SessionId == sessionId.Value)
            .OrderByDescending(snap => snap.CapturedAt)
            .FirstOrDefaultAsync(ct);
    }

    public Task<bool> HasActiveSessionForBookingAsync(Guid tourBookingId, CancellationToken ct = default)
        => context.LiveTrackingSessions
            .AnyAsync(
                s => s.TourBookingId == tourBookingId
                    && (s.Status == SessionStatus.Active || s.Status == SessionStatus.Paused),
                ct);

    public async Task<IReadOnlyList<LiveTrackingSession>> GetSessionHistoryByUserIdAsync(
        Guid userId,
        SessionStatus[]? statuses,
        DateTime? cursorStartedAt,
        Guid? cursorId,
        int limit,
        CancellationToken ct = default)
    {
        var query = context.LiveTrackingSessions
            .AsNoTracking()
            .Where(s => s.UserId == userId);

        if (statuses is { Length: > 0 })
        {
            var statusFilter = statuses.Distinct().ToArray();
            query = query.Where(s => statusFilter.Contains(s.Status));
        }

        // Cursor-based pagination: rows strictly AFTER cursor (StartedAt DESC, Id ASC)
        if (cursorStartedAt.HasValue && cursorId.HasValue)
        {
            var cursorAt = cursorStartedAt.Value;
            var cursorSessionId = cursorId.Value;
            query = query.Where(s =>
                s.StartedAt < cursorAt
                || (s.StartedAt == cursorAt && s.Id.CompareTo(cursorSessionId) > 0));
        }

        return await query
            .OrderByDescending(s => s.StartedAt)
            .ThenBy(s => s.Id)
            .Take(limit)
            .ToListAsync(ct);
    }
}
