using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Booking.Application.Commands.UpdateAvailabilitySlot;

public sealed record UpdateAvailabilitySlotCommand(
    Guid SlotId,
    DateOnly Date,
    TimeOnly StartTime,
    TimeOnly EndTime,
    int MaxCapacity,
    decimal? PriceOverride,
    string? PriceOverrideCurrency) : ICommand;
