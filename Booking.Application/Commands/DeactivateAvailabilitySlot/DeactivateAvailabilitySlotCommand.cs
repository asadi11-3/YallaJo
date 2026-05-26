using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Booking.Application.Commands.DeactivateAvailabilitySlot;

/// <summary>Guide deactivates an availability slot (no new bookings).</summary>
public sealed record DeactivateAvailabilitySlotCommand(Guid SlotId) : ICommand;
