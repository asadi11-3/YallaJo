using Booking.Application.Commands.Common;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Booking.Application.Commands.UpdateAvailabilitySlot;

public sealed record UpdateAvailabilitySlotCommand(
    Guid SlotId,
    int MaxCapacity,
    byte[] RowVersion) : ICommand<AvailabilitySlotDto>;
