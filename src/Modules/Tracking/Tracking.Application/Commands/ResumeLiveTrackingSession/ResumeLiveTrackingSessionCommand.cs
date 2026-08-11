using Tracking.Application.Common;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Tracking.Application.Commands.ResumeLiveTrackingSession;

public sealed record ResumeLiveTrackingSessionCommand(
    Guid SessionId,
    DateTime ResumedAt) : ICommand<TrackingSessionSummaryDto>;
