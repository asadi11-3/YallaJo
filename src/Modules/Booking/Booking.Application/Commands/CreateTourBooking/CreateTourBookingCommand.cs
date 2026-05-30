using Booking.Domain.Enums;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Booking.Application.Commands.CreateTourBooking;

/// <summary>
/// Per-tier participant breakdown used to compute pricing and total seats.
/// </summary>
public sealed record ParticipantBreakdown(int Adult, int Child, int Infant, int Senior)
{
    public int Total => Adult + Child + Infant + Senior;

    public IEnumerable<(TierType Tier, int Count)> NonEmptyTiers()
    {
        if (Adult > 0)
        {
            yield return (TierType.Adult, Adult);
        }

        if (Child > 0)
        {
            yield return (TierType.Child, Child);
        }

        if (Infant > 0)
        {
            yield return (TierType.Infant, Infant);
        }

        if (Senior > 0)
        {
            yield return (TierType.Senior, Senior);
        }
    }
}

/// <summary>
/// Creates a new tour booking in <see cref="BookingStatus.AwaitingPayment"/>.
/// </summary>
public sealed record CreateTourBookingCommand(
    Guid TourId,
    Guid GuideId,
    Guid AvailabilitySlotId,
    ParticipantBreakdown ParticipantBreakdown,
    bool IsPrivate,
    string? PromoCode,
    int LoyaltyPointsToRedeem,
    string? SpecialRequests)
    : ICommand<CreateTourBookingResult>;
