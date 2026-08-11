using Tracking.Application.Commands.ReachCheckpoint;

namespace Tracking.Presentation.Endpoints.TrakcingSession.Models;

public sealed record ReachCheckpointRequest(
    DateTime ReachedAt,
    string? Notes)
{
    public ReachCheckpointCommand ToCommand(Guid sessionId, Guid checkpointId)
        => new(sessionId, checkpointId, ReachedAt, Notes);
}
