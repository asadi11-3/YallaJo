using Tracking.Application.Commands.SkipCheckpoint;

namespace Tracking.Presentation.Endpoints.TrakcingSession.Models;


public sealed record SkipCheckpointRequest(string? Notes)
{
    public SkipCheckpointCommand ToCommand(Guid sessionId, Guid checkpointId)
        => new(sessionId, checkpointId, Notes);
}
