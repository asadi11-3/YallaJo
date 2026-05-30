using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Booking.Application.Commands.DeleteAvailabilitySlot;

public sealed record DeleteAvailabilitySlotCommand(
    Guid SlotId,
    byte[] RowVersion) : ICommand;
