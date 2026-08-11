using Tracking.Application.Common;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Tracking.Application.Commands.SkipCheckpoint;

public sealed record SkipCheckpointCommand(
    Guid SessionId,
    Guid CheckpointId,
    string? Notes) : ICommand<TrackingCheckpointDto>;
