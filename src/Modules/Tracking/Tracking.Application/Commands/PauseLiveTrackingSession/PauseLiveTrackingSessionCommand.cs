using Tracking.Application.Common;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Tracking.Application.Commands.PauseLiveTrackingSession;

public sealed record PauseLiveTrackingSessionCommand(
    Guid SessionId,
    DateTime PausedAt) : ICommand<TrackingSessionSummaryDto>;
