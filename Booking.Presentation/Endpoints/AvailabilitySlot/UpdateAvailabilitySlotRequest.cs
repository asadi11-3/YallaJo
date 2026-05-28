using Booking.Application.Commands.UpdateAvailabilitySlot;

namespace Booking.Presentation.Endpoints.AvailabilitySlot;

public sealed record UpdateAvailabilitySlotRequest(
    int MaxCapacity,
    string RowVersion)
{
    public UpdateAvailabilitySlotCommand ToCommand(Guid slotId)
    {
        byte[] decoded;
        try
        {
            decoded = string.IsNullOrWhiteSpace(RowVersion)
                ? []
                : Convert.FromBase64String(RowVersion);
        }
        catch (FormatException)
        {
            decoded = [];
        }

        return new UpdateAvailabilitySlotCommand(slotId, MaxCapacity, decoded);
    }
}
