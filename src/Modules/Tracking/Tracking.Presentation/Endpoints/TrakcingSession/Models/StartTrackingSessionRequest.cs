using Tracking.Application.Commands.StartLiveTrackingSession;

namespace Tracking.Presentation.Endpoints.TrakcingSession.Models;

public sealed record StartTrackingSessionRequest(
    Guid UserId,
    Guid TourBookingId,
    Guid TourGuideId,
    DateTime StartedAt,
    IReadOnlyList<Guid>? WaypointIds)
{
    public StartLiveTrackingSessionCommand ToCommand()
        => new(UserId, TourBookingId, TourGuideId, StartedAt, WaypointIds);
}
