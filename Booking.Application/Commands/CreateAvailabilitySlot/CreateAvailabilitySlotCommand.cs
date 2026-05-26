using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Booking.Application.Commands.CreateAvailabilitySlot;

/// <summary>Guide creates an availability slot for a tour.</summary>
public sealed record CreateAvailabilitySlotCommand(
    Guid TourId,
    DateOnly Date,
    TimeOnly StartTime,
    TimeOnly EndTime,
    int MaxCapacity,
    decimal? PriceOverride,
    string? PriceOverrideCurrency)
    : ICommand<Guid>;
