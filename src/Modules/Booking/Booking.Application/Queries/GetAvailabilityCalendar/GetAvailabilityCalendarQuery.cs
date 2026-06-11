using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Booking.Application.Queries.GetAvailabilityCalendar;

/// <summary>
/// [Backend] B3 — per-day availability aggregates for one month of a tour's slots,
/// powering the provider availability calendar (CAL1). Owner/admin scoped.
/// </summary>
public sealed record GetAvailabilityCalendarQuery(Guid TourId, int Year, int Month)
    : IQuery<IReadOnlyList<AvailabilityCalendarDayDto>>;

/// <summary>One row per calendar day that has at least one slot.</summary>
public sealed record AvailabilityCalendarDayDto(
    DateOnly Date,
    int SlotCount,
    int TotalCapacity,
    int BookedSeats,
    bool HasOpenSlots);
