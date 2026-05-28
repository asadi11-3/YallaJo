using Booking.Application.Commands.CreateTourBooking;

namespace Booking.Presentation.Endpoints.TourBooking;

/// <summary>
/// JSON body for <c>POST /api/v1/booking/tour</c>.
/// </summary>
public sealed record CreateTourBookingRequest(
    Guid TourId,
    Guid GuideId,
    Guid AvailabilitySlotId,
    ParticipantBreakdownRequest ParticipantBreakdown,
    bool IsPrivate,
    string? PromoCode,
    int LoyaltyPointsToRedeem,
    string? SpecialRequests)
{
    public CreateTourBookingCommand ToCommand()
        => new(
            TourId: TourId,
            GuideId: GuideId,
            AvailabilitySlotId: AvailabilitySlotId,
            ParticipantBreakdown: ParticipantBreakdown.ToDomain(),
            IsPrivate: IsPrivate,
            PromoCode: PromoCode,
            LoyaltyPointsToRedeem: LoyaltyPointsToRedeem,
            SpecialRequests: SpecialRequests);
}

public sealed record ParticipantBreakdownRequest(int Adult, int Child, int Infant, int Senior)
{
    public ParticipantBreakdown ToDomain()
        => new(Adult, Child, Infant, Senior);
}
