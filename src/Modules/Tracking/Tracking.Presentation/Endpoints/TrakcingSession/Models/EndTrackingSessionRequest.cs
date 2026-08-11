using Tracking.Application.Commands.EndLiveTrackingSession;

namespace Tracking.Presentation.Endpoints.TrakcingSession.Models;

public sealed record EndTrackingSessionRequest(
    string Reason,
    DateTime EndedAt)
{
    public EndLiveTrackingSessionCommand ToCommand(Guid sessionId)
        => new(sessionId, Reason, EndedAt);
}
