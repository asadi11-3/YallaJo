using Booking.Application.Commands.CreateAvailabilitySlot;

namespace Booking.Presentation.Endpoints.AvailabilitySlot;

public sealed record CreateAvailabilitySlotRequest(
    Guid TourId,
    DateOnly Date,
    TimeOnly StartTime,
    TimeOnly EndTime,
    int MaxCapacity)
{
    public CreateAvailabilitySlotCommand ToCommand()
        => new(TourId, Date, StartTime, EndTime, MaxCapacity);
}
