using Booking.Application.Commands.JoinRequest.RejectJoinRequest;

namespace Booking.Presentation.Endpoints.JoinRequest;

public sealed record RejectJoinRequestRequest(string? Reason)
{
    public RejectJoinRequestCommand ToCommand(Guid joinRequestId)
        => new(JoinRequestId: joinRequestId, Reason: Reason);
}
