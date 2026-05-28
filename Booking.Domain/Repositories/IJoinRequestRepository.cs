using Booking.Domain.Entities;
using YallaJo.SharedKernel.Domain.Abstractions.Data;

namespace Booking.Domain.Repositories;

/// <summary>
/// Repository contract for <see cref="JoinRequest"/> aggregates.
/// </summary>
public interface IJoinRequestRepository : IRepository<JoinRequest, Guid>
{
    /// <summary>Lists join requests attached to a specific booking.</summary>
    Task<IReadOnlyList<JoinRequest>> GetByBookingIdAsync(Guid bookingId, CancellationToken ct = default);

    /// <summary>Lists join requests submitted by a specific user.</summary>
    Task<IReadOnlyList<JoinRequest>> GetByUserIdAsync(Guid userId, CancellationToken ct = default);

    /// <summary>Lists pending join requests that have passed their expiry time.</summary>
    Task<IReadOnlyList<JoinRequest>> GetExpiredPendingAsync(DateTime utcNow, CancellationToken ct = default);

    /// <summary>Returns true if the user already has a pending join request for the given booking.</summary>
    Task<bool> HasPendingRequestAsync(Guid tourBookingId, Guid userId, CancellationToken ct = default);
}
