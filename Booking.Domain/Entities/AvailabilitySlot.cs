using Booking.Domain.Enums;
using Booking.Domain.Events;
using YallaJo.SharedKernel.Domain.Entities;
using YallaJo.SharedKernel.Domain.Exceptions;

namespace Booking.Domain.Entities;

public sealed class AvailabilitySlot : AuditableEntity, IAggregateRoot
{
    private AvailabilitySlot() { } // EF Core

    public Guid TourGuideId { get; private set; }
    public SlotType SlotType { get; private set; }
    public Guid? TourId { get; private set; }
    public Guid? BusinessId { get; private set; }
    public DateOnly Date { get; private set; }
    public TimeOnly StartTime { get; private set; }
    public TimeOnly EndTime { get; private set; }
    public int MaxCapacity { get; private set; } = 1;
    public int BookedCount { get; private set; }
    public int LockedCount { get; private set; }
    public decimal? PriceOverride { get; private set; }
    public string? PriceOverrideCurrency { get; private set; }
    public Guid? ScheduleId { get; private set; }
    public Guid? ServiceItemId { get; private set; }
    public bool IsActive { get; private set; } = true;

    public TourGuide TourGuide { get; private set; } = default!;

    /// <summary>
    /// Available seats = MaxCapacity - already booked (paid) - currently locked (in checkout).
    /// </summary>
    public int AvailableCount => MaxCapacity - BookedCount - LockedCount;

    public static AvailabilitySlot CreateForTour(
        Guid tourGuideId,
        Guid tourId,
        DateOnly date,
        TimeOnly start,
        TimeOnly end,
        int maxCapacity)
    {
        return new AvailabilitySlot
        {
            TourGuideId = tourGuideId,
            SlotType = SlotType.Tour,
            TourId = tourId,
            Date = date,
            StartTime = start,
            EndTime = end,
            MaxCapacity = maxCapacity
        };
    }

    public static AvailabilitySlot CreateForBusiness(
        Guid tourGuideId,
        Guid businessId,
        DateOnly date,
        TimeOnly start,
        TimeOnly end,
        int maxCapacity)
    {
        return new AvailabilitySlot
        {
            TourGuideId = tourGuideId,
            SlotType = SlotType.Business,
            BusinessId = businessId,
            Date = date,
            StartTime = start,
            EndTime = end,
            MaxCapacity = maxCapacity
        };
    }

    /// <summary>
    /// Reserves <paramref name="participantCount"/> seats as locked (in checkout).
    /// Throws if slot is inactive or insufficient capacity.
    /// Raises <see cref="AvailabilitySlotCapacityChangedDomainEvent"/>.
    /// </summary>
    public void Lock(int participantCount)
    {
        if (participantCount < 1)
        {
            throw new BusinessRuleViolationException("ParticipantCount must be at least 1.");
        }

        if (!IsActive)
        {
            throw new BusinessRuleViolationException("Cannot lock seats on an inactive slot.");
        }

        if (AvailableCount < participantCount)
        {
            throw new BusinessRuleViolationException(
                $"Insufficient capacity: requested {participantCount}, available {AvailableCount}.");
        }

        var previousAvailable = AvailableCount;
        LockedCount += participantCount;
        MarkUpdated();
        AddDomainEvent(new AvailabilitySlotCapacityChangedDomainEvent(Id, previousAvailable, AvailableCount));
    }

    /// <summary>
    /// Releases previously-locked seats (e.g. payment expired, booking cancelled before payment).
    /// </summary>
    public void ReleaseLock(int participantCount)
    {
        if (participantCount < 1)
        {
            throw new BusinessRuleViolationException("ParticipantCount must be at least 1.");
        }

        if (LockedCount < participantCount)
        {
            throw new BusinessRuleViolationException(
                $"Cannot release {participantCount} locked seats; only {LockedCount} are locked.");
        }

        var previousAvailable = AvailableCount;
        LockedCount -= participantCount;
        MarkUpdated();
        AddDomainEvent(new AvailabilitySlotCapacityChangedDomainEvent(Id, previousAvailable, AvailableCount));
    }

    /// <summary>
    /// Promotes locked seats to confirmed booked seats (payment captured).
    /// Net AvailableCount is unchanged; locks become bookings.
    /// </summary>
    public void ConfirmBooking(int participantCount)
    {
        if (participantCount < 1)
        {
            throw new BusinessRuleViolationException("ParticipantCount must be at least 1.");
        }

        if (LockedCount < participantCount)
        {
            throw new BusinessRuleViolationException(
                $"Cannot confirm {participantCount} seats; only {LockedCount} are locked.");
        }

        LockedCount -= participantCount;
        BookedCount += participantCount;
        MarkUpdated();
    }

    /// <summary>
    /// Releases confirmed booked seats (e.g. confirmed booking later cancelled).
    /// </summary>
    public void ReleaseBooking(int participantCount)
    {
        if (participantCount < 1)
        {
            throw new BusinessRuleViolationException("ParticipantCount must be at least 1.");
        }

        if (BookedCount < participantCount)
        {
            throw new BusinessRuleViolationException(
                $"Cannot release {participantCount} booked seats; only {BookedCount} are booked.");
        }

        var previousAvailable = AvailableCount;
        BookedCount -= participantCount;
        MarkUpdated();
        AddDomainEvent(new AvailabilitySlotCapacityChangedDomainEvent(Id, previousAvailable, AvailableCount));
    }

    public void Deactivate()
    {
        if (!IsActive)
        {
            return;
        }

        IsActive = false;
        MarkUpdated();
    }

    public void Activate()
    {
        if (IsActive)
        {
            return;
        }

        IsActive = true;
        MarkUpdated();
    }
}
