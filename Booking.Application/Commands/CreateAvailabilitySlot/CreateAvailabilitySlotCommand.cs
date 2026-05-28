using Booking.Application.Commands.Common;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Booking.Application.Commands.CreateAvailabilitySlot;

public sealed record CreateAvailabilitySlotCommand(
    Guid TourId,
    DateOnly Date,
    TimeOnly StartTime,
    TimeOnly EndTime,
    int MaxCapacity) : ICommand<AvailabilitySlotDto>;
