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

    Task<bool> ExistsPendingForUserAndBookingAsync(
        Guid bookingId,
        Guid userId,
        CancellationToken ct = default);

    Task<JoinRequest?> GetByIdTrackedAsync(Guid id, CancellationToken ct = default);
}
