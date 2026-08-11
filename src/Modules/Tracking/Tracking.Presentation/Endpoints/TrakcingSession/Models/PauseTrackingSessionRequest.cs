using Tracking.Application.Commands.PauseLiveTrackingSession;

namespace Tracking.Presentation.Endpoints.TrakcingSession.Models;

public sealed record PauseTrackingSessionRequest(DateTime PausedAt)
{
    public PauseLiveTrackingSessionCommand ToCommand(Guid sessionId)
        => new(sessionId, PausedAt);
}
