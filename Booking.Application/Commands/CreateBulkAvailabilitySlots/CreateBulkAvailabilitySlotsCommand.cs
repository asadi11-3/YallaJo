using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Booking.Application.Commands.CreateBulkAvailabilitySlots;

public enum AvailabilityRecurrence
{
    Daily = 0,
    Weekly = 1,
    Custom = 2,
}

public sealed record CreateBulkAvailabilitySlotsCommand(
    Guid TourId,
    DateOnly StartDate,
    DateOnly EndDate,
    AvailabilityRecurrence Recurrence,
    IReadOnlyList<DayOfWeek>? DaysOfWeek,
    TimeOnly StartTime,
    TimeOnly EndTime,
    int MaxCapacity,
    bool SkipExisting = true) : ICommand<CreateBulkAvailabilitySlotsResult>;
