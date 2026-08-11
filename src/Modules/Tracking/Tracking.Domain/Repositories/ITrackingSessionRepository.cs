using Tracking.Domain.Entities;
using Tracking.Domain.Enums;
using YallaJo.SharedKernel.Domain.Abstractions.Data;

namespace Tracking.Domain.Repositories;

public interface ITrackingSessionRepository : IRepository<LiveTrackingSession, Guid>
{

    Task<LiveTrackingSession?> GetByIdWithDetailsAsync(Guid id, CancellationToken ct = default);


    Task<LiveTrackingSession?> GetActiveByBookingIdAsync(Guid tourBookingId, CancellationToken ct = default);


    Task<IReadOnlyList<LiveTrackingSession>> GetByBookingIdAsync(Guid tourBookingId, CancellationToken ct = default);

    Task<IReadOnlyList<LiveTrackingSession>> GetActiveSessionsByGuideIdAsync(Guid tourGuideId, CancellationToken ct = default);

    Task<LocationSnapshot?> GetLatestSnapshotByBookingIdAsync(Guid tourBookingId, CancellationToken ct = default);

    Task<bool> HasActiveSessionForBookingAsync(Guid tourBookingId, CancellationToken ct = default);

    Task<IReadOnlyList<LiveTrackingSession>> GetSessionHistoryByUserIdAsync(
        Guid userId,
        SessionStatus[]? statuses,
        DateTime? cursorStartedAt,
        Guid? cursorId,
        int limit,
        CancellationToken ct = default);
}
