using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Booking.Application.Queries.GetAvailabilitySlots;

/// <summary>Gets availability slots for a tour or guide within a date range.</summary>
public sealed record GetAvailabilitySlotsQuery(
    Guid? TourId,
    Guid? TourGuideId,
    DateOnly? FromDate,
    DateOnly? ToDate)
    : IQuery<IReadOnlyList<AvailabilitySlotDto>>;
