using Booking.Domain.Entities;
using YallaJo.SharedKernel.Domain.Abstractions.Data;

namespace Booking.Domain.Repositories;

/// <summary>
/// Repository contract for <see cref="SlotLock"/> aggregates.
/// Used during the 5-step booking engine to hold a slot capacity for a user
/// between checkout-start and payment-confirm.
/// </summary>
public interface ISlotLockRepository : IRepository<SlotLock, Guid>
{
    /// <summary>Returns the active (non-released, non-expired) lock for a user+slot, or null.</summary>
    Task<SlotLock?> GetActiveByUserAndSlotAsync(Guid userId, Guid slotId, CancellationToken ct = default);

    /// <summary>Lists locks that have expired but are not yet released.</summary>
    Task<IReadOnlyList<SlotLock>> GetExpiredLocksAsync(DateTime now, CancellationToken ct = default);
}
