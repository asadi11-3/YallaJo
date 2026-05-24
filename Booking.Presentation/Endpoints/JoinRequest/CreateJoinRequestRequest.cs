using Booking.Application.Commands.JoinRequest.CreateJoinRequest;

namespace Booking.Presentation.Endpoints.JoinRequest;

public sealed record CreateJoinRequestRequest(
    Guid BookingId,
    int ParticipantCount,
    string? Message)
{
    public CreateJoinRequestCommand ToCommand()
        => new(
            BookingId: BookingId,
            ParticipantCount: ParticipantCount,
            Message: Message);
}
