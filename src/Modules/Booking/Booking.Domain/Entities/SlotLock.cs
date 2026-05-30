using Booking.Domain.Events;
using YallaJo.SharedKernel.Domain.Entities;
using YallaJo.SharedKernel.Domain.Exceptions;

namespace Booking.Domain.Entities;

public sealed class SlotLock : BaseEntity, IAggregateRoot
{
    private SlotLock() { } // EF Core

    public Guid AvailabilitySlotId { get; private set; }
    public Guid UserId { get; private set; }
    public Guid? BookingId { get; private set; }
    public int ParticipantCount { get; private set; }
    public DateTime LockedAt { get; private set; }
    public DateTime ExpiresAt { get; private set; }
    public bool IsReleased { get; private set; }
    public DateTime? ReleasedAt { get; private set; }

    public AvailabilitySlot AvailabilitySlot { get; private set; } = default!;

    public static SlotLock Create(
        Guid userId,
        Guid availabilitySlotId,
        int participantCount,
        TimeSpan ttl,
        Guid? bookingId = null)
    {
        if (userId == Guid.Empty)
        {
            throw new BusinessRuleViolationException("UserId is required.");
        }

        if (availabilitySlotId == Guid.Empty)
        {
            throw new BusinessRuleViolationException("AvailabilitySlotId is required.");
        }

        if (participantCount < 1)
        {
            throw new BusinessRuleViolationException("ParticipantCount must be at least 1.");
        }

        if (ttl <= TimeSpan.Zero)
        {
            throw new BusinessRuleViolationException("TTL must be positive.");
        }

        var now = DateTime.UtcNow;
        var slotLock = new SlotLock
        {
            UserId = userId,
            AvailabilitySlotId = availabilitySlotId,
            BookingId = bookingId,
            ParticipantCount = participantCount,
            LockedAt = now,
            ExpiresAt = now.Add(ttl),
            IsReleased = false
        };

        slotLock.AddDomainEvent(new SlotLockCreatedDomainEvent(
            slotLock.Id, availabilitySlotId, userId, slotLock.ExpiresAt));

        return slotLock;
    }

    public void AttachBookingId(Guid bookingId)
    {
        if (bookingId == Guid.Empty)
        {
            throw new BusinessRuleViolationException("BookingId is required.");
        }

        if (BookingId.HasValue && BookingId.Value != bookingId)
        {
            throw new BusinessRuleViolationException(
                $"SlotLock is already attached to booking {BookingId.Value}.");
        }

        BookingId = bookingId;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Release()
    {
        if (IsReleased)
        {
            return;
        }

        IsReleased = true;
        ReleasedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
        AddDomainEvent(new SlotLockReleasedDomainEvent(Id, AvailabilitySlotId));
    }

    public bool IsExpired(DateTime now) => !IsReleased && now >= ExpiresAt;
}
