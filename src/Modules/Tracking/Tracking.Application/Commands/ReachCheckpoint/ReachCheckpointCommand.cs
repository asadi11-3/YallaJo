using Tracking.Application.Common;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Tracking.Application.Commands.ReachCheckpoint;

public sealed record ReachCheckpointCommand(
    Guid SessionId,
    Guid CheckpointId,
    DateTime ReachedAt,
    string? Notes) : ICommand<TrackingCheckpointDto>;
