using YallaJo.SharedKernel.Domain.Entities;

namespace Booking.Domain.Entities;

public sealed class SlotLock : BaseEntity, IAggregateRoot
{
    private SlotLock() { } // EF Core

    public Guid AvailabilitySlotId { get; private set; }
    public Guid UserId { get; private set; }
    public DateTime LockedAt { get; private set; }
    public DateTime ExpiresAt { get; private set; }
    public bool IsReleased { get; private set; }
    public DateTime? ReleasedAt { get; private set; }

    public AvailabilitySlot AvailabilitySlot { get; private set; } = default!;
}
