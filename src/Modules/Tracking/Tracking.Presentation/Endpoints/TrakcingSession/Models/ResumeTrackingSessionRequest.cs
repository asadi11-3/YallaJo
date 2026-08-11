using Tracking.Application.Commands.ResumeLiveTrackingSession;

namespace Tracking.Presentation.Endpoints.TrakcingSession.Models;

public sealed record ResumeTrackingSessionRequest(DateTime ResumedAt)
{
    public ResumeLiveTrackingSessionCommand ToCommand(Guid sessionId)
        => new(sessionId, ResumedAt);
}
