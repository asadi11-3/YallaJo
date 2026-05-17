using Booking.Domain.Enums;
using YallaJo.SharedKernel.Domain.Entities;

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
}
